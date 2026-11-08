using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainGame : MonoBehaviour
{
    public GameObject rolePanel; // Tham chiếu đến Panel UI trong Inspector
    public float panelHideDelay = 5f; // Thời gian ẩn panel sau khi game bắt đầu (tính bằng giây)

    private void Start()
    {
        if (rolePanel == null)
        {
            return;
        }

        // Không có phiên mạng (mở thẳng scene MainGame trong Editor để thử)
        // thì Singleton là null và bản cũ sẽ ném NullReferenceException ngay đây.
        if (NetworkManager.Singleton == null)
        {
            rolePanel.SetActive(false);
            return;
        }

        // Kiểm tra xem người chơi có phải là host không
        if (NetworkManager.Singleton.IsHost)
        {
            // Nếu là host, ẩn ngay lập tức
            rolePanel.SetActive(false);
        }
        else
        {
            // Nếu là client, hiển thị panel trong một khoảng thời gian ngắn rồi ẩn đi
            rolePanel.SetActive(true);
            StartCoroutine(HidePanelAfterDelay(panelHideDelay)); // Chạy coroutine để ẩn panel sau thời gian delay
        }
    }

    // Coroutine để ẩn panel sau một khoảng thời gian
    private IEnumerator HidePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        rolePanel.SetActive(false); // Ẩn panel sau delay
    }

    /// <summary>
    /// Thoát trận giữa chừng. Hiện CHƯA có nút nào trong MainGame.unity gọi hàm này
    /// (quét toàn scene không có m_PersistentCalls nào) — Human cần thêm nút trong
    /// Inspector và trỏ onClick vào đây.
    /// Host bấm thì cả phòng bị ngắt; client bấm thì chỉ mình rời đi.
    /// </summary>
    public void ReturnToMenu()
    {
        NetworkSession.LeaveAndLoad("SelectCharacter");
    }
}
