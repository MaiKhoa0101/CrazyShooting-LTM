using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SetCharacter : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField NamePlayer; // InputField for the player's name

    [SerializeField]
    private GameObject Character; // GameObject for the selected character

    public static string selectedCharacterSprite; // Static sprite to store the character across scenes
    public static string playerNameText; // String to store the player's name
    private Scene currentScene;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Register scene load event
        currentScene = SceneManager.GetActiveScene();
        Debug.Log("Current scene at Start: " + currentScene.name);

        if (currentScene.name == "SelectCharacter")
        {
            // Add listener to the NamePlayer InputField
            if (NamePlayer != null)
            {
                NamePlayer.onValueChanged.AddListener(OnValueChanged); // Setup listener here
            }
            else
            {
                Debug.LogWarning("NamePlayer InputField is not assigned.");
            }
        }
    }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Player name text: "+playerNameText+"selectCharacterSprite: "+selectedCharacterSprite);
        currentScene = scene;
        Debug.Log("Scene loaded: " + scene.name);

        if (scene.name == "JoinRoom")
        {
            playerNameText = PlayerPrefs.GetString("PlayerName", "Guest");
            Debug.Log("Player name loaded: " + playerNameText);
            Debug.Log("Entered JoinRoom scene.");
        }
    }

    // Event listener for TMP_InputField text changes
    private void OnValueChanged(string inputText)
    {
        if (!string.IsNullOrEmpty(inputText))
        {
            playerNameText = inputText;
            PlayerPrefs.SetString("PlayerName", inputText);
            Debug.Log("Player name updated to: " + inputText);

            // Store the character sprite's name if available
            if (Character != null)
            {
                SpriteRenderer spriteRenderer = Character.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    PlayerPrefs.SetString("PlayerCharacter", spriteRenderer.sprite.name);
                    selectedCharacterSprite = spriteRenderer.sprite.name;
                    Debug.Log("Player character sprite stored: " + spriteRenderer.sprite.name);
                }
                else
                {
                    Debug.LogWarning("SpriteRenderer component missing on Character GameObject in SelectCharacter scene.");
                }
            }
        }
        else
        {
            Debug.LogWarning("Player name is empty.");
        }
    }

    // Remove the listener when the object is destroyed
    private void OnDestroy()
    {
        // Phải huỷ cả đăng ký sceneLoaded, không chỉ listener của InputField.
        // Thiếu dòng này thì mỗi lần đổi scene lại để sót một handler chết,
        // số handler tăng dần theo từng vòng chơi lại.
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (NamePlayer != null)
        {
            NamePlayer.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}
