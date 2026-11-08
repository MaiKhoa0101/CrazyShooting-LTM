using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Nơi giữ kết quả trận đấu để scene "GameOver" đọc lại sau khi đã đổi scene.
/// Phải là static vì mọi NetworkObject đều bị huỷ khi chuyển scene,
/// nên không thể mang dữ liệu qua bằng component thường.
/// </summary>
public static class MatchResult
{
    public static bool HasResult;
    public static bool IsDraw;
    public static string WinnerName = string.Empty;

    public static void Set(string winnerName, bool isDraw)
    {
        HasResult = true;
        IsDraw = isDraw;
        WinnerName = winnerName ?? string.Empty;
    }

    public static void Clear()
    {
        HasResult = false;
        IsDraw = false;
        WinnerName = string.Empty;
    }
}

/// <summary>
/// Trọng tài của trận đấu.
///
/// Nguyên tắc: chỉ SERVER quyết định trận kết thúc và ai là người thắng.
/// Client không tự suy ra kết quả, chỉ nhận qua ClientRpc.
///
/// Người thắng được xác định bằng trạng thái mạng thật — duyệt
/// NetworkManager.SpawnManager.SpawnedObjectsList rồi đọc life_count —
/// KHÔNG dùng tag. Tag chỉ dành cho va chạm vật lý; dùng tag để suy ra
/// danh tính người chơi là sai vì nhiều prefab khác cũng mang tag "Player".
///
/// Gắn component này lên Assets/Sprite/Player/Prefab/Player.prefab — đối tượng
/// điều phối mà host spawn đúng một lần khi vào scene MainGame
/// (xem MainGameManager.cs:50).
/// </summary>
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Tooltip("Số giây giữ lại màn chơi để người chơi kịp đọc bảng 'đã thua' trước khi sang màn kết quả.")]
    public float endMatchDelay = 2.5f;

    [Tooltip("Tên scene kết quả. Bắt buộc phải có trong Build Settings.")]
    public string gameOverSceneName = "GameOver";

    // Chỉ server dùng. Chặn việc kết thúc trận hai lần.
    private bool matchEnded;

    // Số người chơi đông nhất từng thấy trong trận này. Cần nhớ lại vì khi một
    // người rời phòng, đối tượng của họ bị despawn và không còn đếm được nữa.
    private int maxPlayersSeen;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        // Vào trận mới thì xoá kết quả của trận trước (quan trọng cho vòng chơi lại).
        MatchResult.Clear();

        if (IsServer)
        {
            matchEnded = false;
            maxPlayersSeen = 0;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// MovingScripts gọi trên server mỗi khi một nhân vật vừa spawn,
    /// để trọng tài biết trận này có bao nhiêu người.
    /// </summary>
    public void NotifyPlayerSpawned()
    {
        if (!IsServer)
        {
            return;
        }
        maxPlayersSeen = Mathf.Max(maxPlayersSeen, GetPlayers().Count);
    }

    /// <summary>
    /// Một người rời phòng giữa trận. Nếu chỉ còn đúng một người thì người đó thắng,
    /// thay vì để trận treo vô hạn như trước.
    /// </summary>
    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer || matchEnded)
        {
            return;
        }
        StartCoroutine(EvaluateNextFrame());
    }

    private IEnumerator EvaluateNextFrame()
    {
        // Chờ một khung hình để Netcode despawn xong đối tượng của người vừa rời.
        yield return null;
        EvaluateMatchEnd();
    }

    /// <summary>
    /// SERVER gọi hàm này mỗi khi một người chơi vừa hết mạng.
    /// Gọi từ MovingScripts.ReportFallServerRpc().
    /// </summary>
    public void OnPlayerDied()
    {
        EvaluateMatchEnd();
    }

    /// <summary>
    /// Luật kết thúc trận. Chạy khi có người hết mạng HOẶC có người rời phòng.
    /// </summary>
    private void EvaluateMatchEnd()
    {
        if (!IsServer || matchEnded)
        {
            return;
        }

        List<MovingScripts> players = GetPlayers();
        maxPlayersSeen = Mathf.Max(maxPlayersSeen, players.Count);

        List<MovingScripts> alive = players.FindAll(p => p.IsAlive);

        if (maxPlayersSeen >= 2 && alive.Count == 1)
        {
            // Trận từng có ít nhất 2 người và giờ chỉ còn 1 người sống
            // (do chết hết, hoặc do những người kia đã rời phòng).
            EndMatch(ResolveName(alive[0]), false);
        }
        else if (alive.Count == 0 && players.Count > 0)
        {
            // Tất cả cùng hết mạng (hoặc chỉ có một người và người đó vừa chết).
            EndMatch(string.Empty, true);
        }
        // Còn từ 2 người sống trở lên, hoặc không còn ai trong phòng: không kết thúc.
    }

    private void EndMatch(string winnerName, bool isDraw)
    {
        matchEnded = true;

        // Ghi thẳng trên server để không phụ thuộc vào thứ tự tới của ClientRpc.
        MatchResult.Set(winnerName, isDraw);

        Debug.Log($"[MatchManager] Trận kết thúc. isDraw={isDraw}, winner='{winnerName}'");

        AnnounceResultClientRpc(winnerName, isDraw);
        StartCoroutine(LoadGameOverAfterDelay());
    }

    private IEnumerator LoadGameOverAfterDelay()
    {
        yield return new WaitForSeconds(endMatchDelay);

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
        {
            yield break;
        }

        if (nm.SceneManager == null)
        {
            Debug.LogError("[MatchManager] NetworkManager.SceneManager đang null. " +
                           "Kiểm tra 'Enable Scene Management' trên NetworkManager.");
            yield break;
        }

        // BẮT BUỘC đi qua NetworkManager.SceneManager.
        // Nếu gọi SceneManager.LoadScene thẳng thì chỉ máy chủ đổi scene,
        // client sẽ kẹt lại ở MainGame.
        SceneEventProgressStatus status =
            nm.SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"[MatchManager] Không chuyển được sang scene '{gameOverSceneName}'. " +
                           $"Trạng thái: {status}. Kiểm tra scene đã có trong Build Settings chưa.");
        }
    }

    [ClientRpc]
    private void AnnounceResultClientRpc(string winnerName, bool isDraw)
    {
        MatchResult.Set(winnerName, isDraw);
        Debug.Log($"[MatchManager] Nhận kết quả trận: isDraw={isDraw}, winner='{winnerName}'");
    }

    /// <summary>
    /// Lấy mọi nhân vật người chơi đang spawn trên mạng.
    /// Dựa vào SpawnManager (trạng thái mạng thật), không dùng tag.
    /// </summary>
    private static List<MovingScripts> GetPlayers()
    {
        var result = new List<MovingScripts>();

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null)
        {
            return result;
        }

        foreach (NetworkObject netObj in nm.SpawnManager.SpawnedObjectsList)
        {
            if (netObj == null)
            {
                continue;
            }

            var mover = netObj.GetComponent<MovingScripts>();
            if (mover != null)
            {
                result.Add(mover);
            }
        }

        return result;
    }

    private static string ResolveName(MovingScripts player)
    {
        var setName = player.GetComponent<SetName>();
        if (setName != null)
        {
            string networkName = setName.PlayerName;
            if (!string.IsNullOrWhiteSpace(networkName))
            {
                return networkName;
            }
        }

        // Dự phòng khi tên chưa kịp đồng bộ.
        return player.gameObject.name;
    }
}
