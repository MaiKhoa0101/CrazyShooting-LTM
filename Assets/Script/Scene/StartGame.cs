using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class StartGame : MonoBehaviour
{
    public Button startButton; // Tham chiếu đến nút trong Inspector
    public Button startClient; // Tham chiếu đến nút trong Inspector
    private void Start()
    {
        // Script này còn được gắn trên nút "Replay" của scene GameOver chỉ để giữ
        // các hàm cho onClick, và ở đó startClient KHÔNG được gán (= null).
        // Bản cũ không kiểm tra null nên Start() ném NullReferenceException ngay khi
        // mở scene GameOver, và trước lúc ném còn kịp ẩn mất chính nút Replay.
        // Chỉ đụng vào UI khi cả hai nút đều được gán, tức khi đang ở phòng chờ.
        if (startButton == null || startClient == null)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;
        startButton.gameObject.SetActive(isHost);
        startClient.gameObject.SetActive(!isHost);
    }
    
    public void NextScene()
    {
        NetworkManager nm = NetworkManager.Singleton;

        // Host là người mở trận. Phải đi qua NetworkManager.SceneManager để client
        // được kéo sang MainGame cùng lúc. Nếu mỗi máy tự gọi SceneManager.LoadScene
        // thì hai bên vào scene độc lập nhau và Netcode không còn biết ai đang ở đâu.
        if (nm != null && nm.IsServer && nm.SceneManager != null)
        {
            SceneEventProgressStatus status =
                nm.SceneManager.LoadScene("MainGame", LoadSceneMode.Single);

            if (status == SceneEventProgressStatus.Started)
            {
                return;
            }

            Debug.LogError($"[StartGame] Không mở được MainGame qua mạng ({status}). " +
                           "Tạm nạp cục bộ để không kẹt ở phòng chờ.");
            SceneManager.LoadScene("MainGame");
            return;
        }

        // Client không tự mở trận: chờ host kéo sang.
        if (nm != null && nm.IsClient && !nm.IsServer)
        {
            Debug.Log("[StartGame] Chỉ host mới bắt đầu được trận. Đang chờ host.");
            return;
        }

        // Không có phiên mạng nào (ví dụ chạy thử một mình): nạp cục bộ.
        SceneManager.LoadScene("MainGame");
    }
    /// <summary>
    /// Nút "Exit" của phòng chờ. Phải TẮT phiên mạng trước khi rời,
    /// nếu không host vẫn lắng nghe và client vẫn kết nối dù đã ra khỏi phòng.
    /// </summary>
    public void ReturnToMenu()
    {
        NetworkSession.LeaveAndLoad("SelectCharacter");
    }
}