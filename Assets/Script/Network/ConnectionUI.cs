using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lớp phủ báo trạng thái kết nối cho người chơi: "đang kết nối", lỗi nhập sai IP,
/// mất kết nối với host — kèm một nút thoát về menu.
///
/// Toàn bộ giao diện được dựng BẰNG CODE lúc chạy, cố ý như vậy để không phải
/// sửa file .unity nào và Human không phải làm thao tác Inspector nào.
///
/// Trước đây các lỗi này chỉ đi vào Debug.LogError (JoinRoomManager.cs:140, :150),
/// tức người chơi bản build không nhìn thấy gì cả.
/// </summary>
public class ConnectionUI : MonoBehaviour
{
    private static ConnectionUI _instance;

    private GameObject panel;
    private TextMeshProUGUI messageText;
    private GameObject menuButtonObject;

    // NetworkManager đang được theo dõi. Nó bị huỷ và tạo lại mỗi lần rời phiên,
    // nên phải bám theo chứ không đăng ký một lần rồi thôi.
    private NetworkManager watched;

    public static ConnectionUI Instance
    {
        get
        {
            EnsureExists();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    private static void EnsureExists()
    {
        if (_instance != null)
        {
            return;
        }

        var host = new GameObject("ConnectionUI");
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<ConnectionUI>();
    }

    // ------------------------------------------------------------------ API

    /// <summary>Hiện thông báo tạm (ví dụ "Đang kết nối..."), không có nút.</summary>
    public void ShowStatus(string message)
    {
        Display(message, false);
    }

    /// <summary>Hiện lỗi kèm nút "Về menu" để người chơi luôn có đường thoát.</summary>
    public void ShowError(string message)
    {
        Display(message, true);
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // --------------------------------------------------------------- Nội bộ

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        BuildUI();
        Hide();
    }

    private void Update()
    {
        // NetworkManager bị Destroy mỗi lần rời phiên (NetworkSession.LeaveAndLoad)
        // rồi scene SelectCharacter tạo cái mới, nên phải bám theo từng cái một.
        NetworkManager current = NetworkManager.Singleton;
        if (current == watched)
        {
            return;
        }

        Unsubscribe(watched);
        watched = current;
        Subscribe(watched);
    }

    private void OnDestroy()
    {
        Unsubscribe(watched);
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Subscribe(NetworkManager nm)
    {
        if (nm == null)
        {
            return;
        }
        nm.OnClientConnectedCallback += HandleClientConnected;
        nm.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void Unsubscribe(NetworkManager nm)
    {
        if (nm == null)
        {
            return;
        }
        nm.OnClientConnectedCallback -= HandleClientConnected;
        nm.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || clientId != nm.LocalClientId)
        {
            return;
        }
        Hide();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            return;
        }

        // Trên server, callback này còn chạy cho từng client khác rời đi.
        // Chỉ báo lỗi khi CHÍNH máy này bị ngắt, và chỉ khi mình không phải host.
        if (clientId != nm.LocalClientId || nm.IsServer)
        {
            return;
        }

        string reason = nm.DisconnectReason;
        string message = string.IsNullOrWhiteSpace(reason)
            ? "Mất kết nối với host.\nCó thể host đã thoát hoặc mạng bị gián đoạn."
            : "Mất kết nối với host.\nLý do: " + reason;

        ShowError(message);
    }

    private void Display(string message, bool withMenuButton)
    {
        if (panel == null)
        {
            BuildUI();
        }

        panel.SetActive(true);
        messageText.text = message;
        menuButtonObject.SetActive(withMenuButton);

        if (withMenuButton)
        {
            EnsureEventSystem();
        }
    }

    /// <summary>Không có EventSystem thì nút bấm sẽ không nhận được click.</summary>
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var go = new GameObject("ConnectionUI_EventSystem");
        DontDestroyOnLoad(go);
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        return font;
    }

    private void BuildUI()
    {
        TMP_FontAsset font = ResolveFont();

        var canvasObject = new GameObject("ConnectionUI_Canvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // nằm trên mọi UI khác của game

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        // Nền mờ phủ kín màn hình, cũng để chặn click xuyên xuống dưới.
        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        Stretch(panelRect);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        // Dòng chữ thông báo.
        var textObject = new GameObject("Message");
        textObject.transform.SetParent(panel.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(1400f, 400f);
        textRect.anchoredPosition = new Vector2(0f, 80f);

        messageText = textObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            messageText.font = font;
        }
        messageText.fontSize = 46f;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.text = string.Empty;

        // Nút thoát về menu.
        menuButtonObject = new GameObject("MenuButton");
        menuButtonObject.transform.SetParent(panel.transform, false);
        var buttonRect = menuButtonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(440f, 110f);
        buttonRect.anchoredPosition = new Vector2(0f, -160f);

        var buttonImage = menuButtonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.85f, 0.25f, 0.22f, 1f);

        var button = menuButtonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(OnMenuButtonPressed);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(menuButtonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        Stretch(labelRect);

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }
        label.fontSize = 40f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = "VỀ MENU";
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnMenuButtonPressed()
    {
        Hide();
        NetworkSession.LeaveAndLoad("MainMenu");
    }
}
