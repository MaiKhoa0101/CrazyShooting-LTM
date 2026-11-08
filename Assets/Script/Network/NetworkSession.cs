using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Các thao tác vòng đời phiên mạng dùng chung cho mọi scene:
/// rời trận cho sạch, và mở ván mới.
///
/// Gom vào một chỗ vì trước đây mỗi scene tự gọi SceneManager.LoadScene mà
/// không bao giờ tắt NetworkManager, nên phiên mạng cũ vẫn chạy ngầm sau khi
/// người chơi đã rời trận.
/// </summary>
public static class NetworkSession
{
    /// <summary>Đang ở trong một phiên mạng (host hoặc client) hay không.</summary>
    public static bool IsActive
    {
        get
        {
            NetworkManager nm = NetworkManager.Singleton;
            return nm != null && (nm.IsListening || nm.IsClient || nm.IsServer);
        }
    }

    /// <summary>Máy này có quyền điều khiển trận đấu (là host/server) hay không.</summary>
    public static bool IsHost
    {
        get
        {
            NetworkManager nm = NetworkManager.Singleton;
            return nm != null && nm.IsServer;
        }
    }

    /// <summary>
    /// Rời phiên mạng cho SẠCH rồi về scene chỉ định.
    /// Dùng được cho cả host lẫn client.
    /// </summary>
    public static void LeaveAndLoad(string sceneName)
    {
        // Gỡ đóng băng trước tiên: nếu còn kẹt timeScale = 0 thì scene sau sẽ đứng im.
        Time.timeScale = 1f;
        MatchResult.Clear();

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null)
        {
            if (nm.IsListening || nm.IsClient || nm.IsServer)
            {
                // Host tắt thì mọi client sẽ nhận được sự kiện ngắt kết nối.
                nm.Shutdown();
            }

            // BẮT BUỘC huỷ hẳn GameObject, không chỉ Shutdown.
            // NetworkManager.OnEnable() gọi DontDestroyOnLoad vô điều kiện
            // (NetworkManager.cs:1098-1101 trong package 2.13.3), nên đối tượng này
            // sống xuyên mọi scene. Mà scene SelectCharacter lại có sẵn một prefab
            // NetworkManger nữa. Không huỷ thì mỗi lần quay lại sẽ chồng thêm một
            // NetworkManager, kéo theo JoinRoomManager và GameNetworkManager bị nhân
            // bản -> đăng ký SceneManager.sceneLoaded nhiều lần -> spawn trùng người chơi.
            UnityEngine.Object.Destroy(nm.gameObject);
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Huỷ một đối tượng cho đúng cách, dù nó là đối tượng mạng hay đối tượng thường.
    ///
    /// Netcode CẤM client gọi Destroy() lên một NetworkObject đã spawn — làm vậy sẽ
    /// ném lỗi và khiến hai máy lệch trạng thái. Nhưng các script đạn
    /// (BulletPistol, BulletSniper, SlashingLine, Bom) chạy trên MỌI máy và đều
    /// gọi Destroy(gameObject) khi hết thời gian sống.
    ///
    /// Hàm này xử lý: đối tượng mạng thì chỉ server được despawn, client đứng yên
    /// chờ server; đối tượng thường (ví dụ hiệu ứng nổ cục bộ) thì Destroy như cũ.
    /// </summary>
    public static void DespawnOrDestroy(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        var netObj = target.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                netObj.Despawn(true);
            }
            // Client: không làm gì. Server sẽ phát lệnh despawn xuống.
            return;
        }

        UnityEngine.Object.Destroy(target);
    }

    /// <summary>
    /// Host mở một ván mới: kéo cả phòng quay lại scene trận đấu.
    /// Trả về false nếu máy này không phải host hoặc không mở được scene.
    /// </summary>
    public static bool StartRematch(string matchSceneName = "MainGame")
    {
        Time.timeScale = 1f;

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
        {
            return false;
        }

        if (nm.SceneManager == null)
        {
            Debug.LogError("[NetworkSession] NetworkManager.SceneManager đang null. " +
                           "Kiểm tra 'Enable Scene Management' trên NetworkManager.");
            return false;
        }

        // Xoá kết quả ván trước để màn kết quả mới không đọc nhầm dữ liệu cũ.
        MatchResult.Clear();

        // Nạp lại MainGame qua mạng sẽ despawn toàn bộ đối tượng của ván cũ,
        // rồi GameNetworkManager.OnSceneLoaded chạy lại chuỗi spawn từ đầu,
        // nên life_count của mọi người tự động về 3.
        SceneEventProgressStatus status =
            nm.SceneManager.LoadScene(matchSceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"[NetworkSession] Không mở lại được scene '{matchSceneName}'. " +
                           $"Trạng thái: {status}");
            return false;
        }

        return true;
    }
}
