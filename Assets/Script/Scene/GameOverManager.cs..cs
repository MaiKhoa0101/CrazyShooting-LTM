using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverCanvas; // Canvas màn hình kết thúc game

    [Tooltip("Ô chữ hiển thị kết quả. Kéo object 'GameOverText ' vào đây.")]
    public TMP_Text resultText;

    void Start()
    {
        // Scene này CHỈ được mở khi trận đã kết thúc (MatchManager gọi
        // NetworkManager.SceneManager.LoadScene), nên bảng kết quả phải hiện NGAY.
        // Bản cũ gọi SetActive(false) ở đây, đó là lý do màn kết quả không bao giờ
        // nhìn thấy được kể cả khi đã vào được scene.
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }

        // Gỡ đóng băng. Nếu trận trước thoát ra lúc Time.timeScale đang là 0
        // thì toàn bộ scene này sẽ đứng im và không bấm được nút nào.
        Time.timeScale = 1f;

        ShowResult();
    }

    /// <summary>
    /// Đọc kết quả do server công bố (MatchManager.AnnounceResultClientRpc đã ghi
    /// vào MatchResult trước khi đổi scene) và vẽ ra màn hình.
    /// Không dùng FindGameObjectWithTag("Player") — nhiều prefab khác cũng mang
    /// tag "Player" nên cách đó chọn nhầm đối tượng.
    /// </summary>
    private void ShowResult()
    {
        if (resultText == null)
        {
            Debug.LogWarning("[GameOverManager] Chưa gán 'Result Text' nên không hiển thị được " +
                             "kết quả. Kéo object 'GameOverText ' vào ô Result Text trong Inspector.");
            return;
        }

        if (!MatchResult.HasResult)
        {
            resultText.text = "Trận đấu đã kết thúc";
            return;
        }

        string result = MatchResult.IsDraw
            ? "Hoà! Không ai thắng."
            : MatchResult.WinnerName + " là người thắng!";

        // Chỉ host mới mở được ván mới, nên nói trước cho client khỏi bấm hụt.
        if (NetworkSession.IsActive && !NetworkSession.IsHost)
        {
            result += "\n(Chờ host bấm Chơi lại)";
        }

        resultText.text = result;
    }

    private void ShowMessage(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
        }
        Debug.Log("[GameOverManager] " + message);
    }

    // Dùng khi muốn hiện bảng kết thúc ngay trong màn chơi (overlay), không đổi scene.
    public void GameOver()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }

        // CỐ Ý không đặt Time.timeScale = 0f nữa.
        // Đây là game nhiều người chơi: đóng băng thời gian cục bộ chỉ làm treo
        // máy này, không dừng được trận trên máy kia, và rất dễ để lại trạng thái
        // timeScale = 0 khi đổi scene khiến game kẹt cứng không gỡ được.
        ShowResult();
    }

    /// <summary>
    /// Nút "Replay" — TÁI ĐẤU, không phải về menu.
    /// Host kéo cả phòng quay lại MainGame; client chỉ chờ.
    /// Bản cũ đưa thẳng về MainMenu và bỏ mặc phiên mạng đang chạy, nên
    /// không thể chơi lại mà không tắt game.
    /// </summary>
    public void RetryGame()
    {
        Time.timeScale = 1f;

        // Không có phiên mạng (mở thẳng scene để thử): giữ hành vi cũ.
        if (!NetworkSession.IsActive)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        if (!NetworkSession.IsHost)
        {
            ShowMessage("Chỉ host mới bắt đầu được ván mới.\nĐang chờ host...");
            return;
        }

        if (!NetworkSession.StartRematch("MainGame"))
        {
            ShowMessage("Không bắt đầu được ván mới.\nHãy rời phòng rồi vào lại.");
        }
    }

    /// <summary>
    /// Nút "Quit" trong scene GameOver đã được nối sẵn tới hàm tên ReturnToMenu
    /// (GameOver.unity:911) nhưng lớp này TRƯỚC ĐÂY KHÔNG CÓ hàm đó, nên nút bấm
    /// không làm gì cả. Hàm này làm nút sống lại: rời phiên mạng cho sạch rồi về menu.
    /// </summary>
    public void ReturnToMenu()
    {
        NetworkSession.LeaveAndLoad("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Game is quitting...");
        Application.Quit();

        // Nếu đang trong Unity Editor, hiển thị thông báo
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

}
