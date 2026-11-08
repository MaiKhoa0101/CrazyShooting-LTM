using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System;

public class JoinRoomManager : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private string ipAddress;
    public UnityTransport transport;
    public TMP_InputField IPAddressHost;
    public TMP_Text IPAddressHostOnScreen;

    void Start()
    {
        ipAddress = "0.0.0.0";
        SetIpAddress();

        hostButton.onClick.AddListener(() =>
        {
            Debug.Log("Host button pressed in Scene 1.");
            StartCoroutine(LoadSceneAndStartHost("JoinRoom"));
        });

        clientButton.onClick.AddListener(() =>
        {
            Debug.Log("Client button pressed in Scene 1.");
            TryJoinAsClient("JoinRoom");
        });

    }

    /// <summary>
    /// Kiểm tra IP TRƯỚC khi kết nối, và chặn luôn nếu sai.
    /// Bản cũ gọi ConnectToServer() (async, không ai chờ kết quả) rồi vẫn
    /// StartClient và đổi scene bất kể đúng sai, nên nhập IP bậy vẫn trôi vào
    /// phòng chờ rồi treo ở đó mà không có thông báo nào.
    /// </summary>
    private void TryJoinAsClient(string sceneName)
    {
        string typed = IPAddressHost != null ? IPAddressHost.text : string.Empty;
        typed = string.IsNullOrEmpty(typed) ? string.Empty : typed.Trim();

        if (string.IsNullOrEmpty(typed))
        {
            ConnectionUI.Instance.ShowError("Bạn chưa nhập địa chỉ IP của host.");
            return;
        }

        if (!IPAddress.TryParse(typed, out _))
        {
            ConnectionUI.Instance.ShowError(
                "Địa chỉ IP không hợp lệ:\n\"" + typed + "\"\n\nHãy nhập dạng 192.168.1.5");
            return;
        }

        ipAddress = typed;
        SetIpAddress();
        StartCoroutine(ConnectAsClient(typed, sceneName));
    }

    /// <summary>
    /// Kết nối có phản hồi: hiện "đang kết nối", chờ tối đa 10 giây,
    /// và CHỈ đổi scene khi đã thật sự kết nối được.
    /// </summary>
    private IEnumerator ConnectAsClient(string ip, string sceneName)
    {
        ConnectionUI.Instance.ShowStatus("Đang kết nối tới " + ip + " ...");

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            ConnectionUI.Instance.ShowError("Không tìm thấy NetworkManager.");
            yield break;
        }

        transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
        {
            ConnectionUI.Instance.ShowError("Không tìm thấy UnityTransport trên NetworkManager.");
            yield break;
        }
        transport.ConnectionData.Address = ip;

        if (!nm.StartClient())
        {
            ConnectionUI.Instance.ShowError("Không khởi động được client.\nHãy thử lại.");
            yield break;
        }

        const float timeout = 10f;
        float elapsed = 0f;

        while (!nm.IsConnectedClient && elapsed < timeout)
        {
            // IsClient tắt giữa chừng nghĩa là đã bị từ chối hoặc ngắt kết nối.
            if (!nm.IsClient)
            {
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!nm.IsConnectedClient)
        {
            string reason = nm.DisconnectReason;
            nm.Shutdown();

            string detail = string.IsNullOrWhiteSpace(reason) ? string.Empty : "\nLý do: " + reason;
            ConnectionUI.Instance.ShowError(
                "Không kết nối được tới " + ip + "." + detail +
                "\n\nKiểm tra: host đã bấm Tạo phòng chưa, hai máy có cùng mạng LAN không, " +
                "và tường lửa có chặn cổng 7777 không.");
            yield break;
        }

        ConnectionUI.Instance.Hide();

        if (IPAddressHostOnScreen != null)
        {
            IPAddressHostOnScreen.text = ip;
        }
        Debug.Log("Client đã kết nối. ID: " + nm.LocalClientId);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void ReturnToMenu()
    {
        // Tắt phiên mạng trước khi về menu, nếu không StartHost()/StartClient()
        // đã gọi trước đó vẫn tiếp tục chạy ngầm.
        NetworkSession.LeaveAndLoad("MainMenu");
    }

    private IEnumerator LoadSceneAndStartHost(string sceneName)
    {
        ConnectionUI.Instance.ShowStatus("Đang tạo phòng...");

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            ConnectionUI.Instance.ShowError("Không tìm thấy NetworkManager.");
            yield break;
        }

        if (!nm.StartHost())
        {
            ConnectionUI.Instance.ShowError(
                "Không tạo được phòng.\nCổng 7777 có thể đang bị chương trình khác chiếm.");
            yield break;
        }

        // GetLocalIPAddress() có thể ném Exception khi máy không có card mạng IPv4
        // (JoinRoomManager.cs:86). Trong coroutine, exception đó sẽ giết luôn
        // coroutine và người chơi kẹt ở màn chọn nhân vật mà không hiểu vì sao.
        ipAddress = SafeGetLocalIPAddress();

        if (IPAddressHostOnScreen != null)
        {
            IPAddressHostOnScreen.text = ipAddress;
        }
        Debug.Log("Host ID: " + nm.LocalClientId + " / IP: " + ipAddress);

        ConnectionUI.Instance.Hide();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("Host started, đã vào phòng chờ.");
    }

    /// <summary>Lấy IPv4 nội bộ, không bao giờ ném exception.</summary>
    private string SafeGetLocalIPAddress()
    {
        try
        {
            return GetLocalIPAddress();
        }
        catch (Exception e)
        {
            Debug.LogError("Không lấy được IPv4 của máy: " + e.Message);
            return "khong-xac-dinh";
        }
    }

    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                IPAddressHostOnScreen.text = ip.ToString();
                IPAddressHostOnScreen.color = Color.red;
                ipAddress = ip.ToString();
                Debug.Log("IP of host is: " + ipAddress);
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    // LoadSceneAndStartClient() cũ đã được thay bằng ConnectAsClient() ở trên.
    // Bản cũ gọi StartClient() rồi mới gán transport.ConnectionData.Address
    // (gán SAU khi đã bắt đầu kết nối), và đổi scene ngay mà không chờ xem
    // kết nối có thành công hay không.

    public void SetIpAddress()
    {
        if (ipAddress == null)
        {
            Debug.Log("IP address not found.");
        }
        else
        {
            Debug.Log("Found IP address: " + ipAddress);
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.Log("Transport component not found on client.");
            }
            else
            {
                transport.ConnectionData.Address = ipAddress;
                Debug.Log("Client IP assigned: " + transport.ConnectionData.Address);
            }
        }
    }

    public async void ConnectToServer(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out IPAddress ip))
        {
            Debug.LogError("Invalid IP address: " + ipAddress);
            return;
        }

        if (await PingAddress(ip))
        {
            Debug.Log("IP address is valid and reachable: " + ipAddress);
        }
        else
        {
            Debug.LogError("Unable to connect to IP address: " + ipAddress);
        }
    }

    private async Task<bool> PingAddress(IPAddress ipAddress)
    {
        try
        {
            using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping())
            {
                PingReply reply = await ping.SendPingAsync(ipAddress.ToString(), 1000);
                return reply.Status == IPStatus.Success;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error during ping: " + e.Message);
            return false;
        }
    }
}
