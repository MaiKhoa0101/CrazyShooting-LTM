using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelection : MonoBehaviour
{
    public GameObject characterImage;  // GameObject that holds the SpriteRenderer for character images
    public Text characterNameText;  // Text to display the character's name
    public TMP_InputField playerNameInput;  // InputField for player name input
    public TMP_InputField roomInput;  // InputField for room name input

    public Sprite[] characters;  // Array of character images
    public string[] characterNames;  // Array of character names

    private int currentIndex = 0;  // Index of the currently selected character

    void Start()
    {
        UpdateCharacter();
    }

    // When "Next" button is clicked
    public void NextCharacter()
    {
        currentIndex++;
        if (currentIndex >= characters.Length)
        {
            currentIndex = 0; // Loop back to the first character
        }
        UpdateCharacter();
    }

    // When "Back" button is clicked
    public void PreviousCharacter()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = characters.Length - 1; // Loop to the last character
        }
        UpdateCharacter();
    }

    // Update the character image and name display
    void UpdateCharacter()
    {
        SpriteRenderer characterSpriteRenderer = characterImage.GetComponent<SpriteRenderer>();
        if (characterSpriteRenderer != null)
        {
            characterSpriteRenderer.sprite = characters[currentIndex]; // Correctly assign the sprite
            characterNameText.text = characterNames[currentIndex];
        }
        else
        {
            Debug.LogError("SpriteRenderer component not found on the characterImage GameObject.");
        }
    }

    // When "Play" button is clicked
    public void PlayGame()
    {
        string playerName = playerNameInput.text;
        string roomName = roomInput.text;

        // Check that player name and room name are not empty
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player name is required.");
            return;
        }

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name is required.");
            return;
        }

        // Save player name, room name, and selected character to PlayerPrefs
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("RoomName", roomName);
        PlayerPrefs.SetString("SelectedCharacter", characterNames[currentIndex]);
        PlayerPrefs.SetInt("SelectedCharacterIndex", currentIndex);

        // Load the JoinRoom scene
        // SceneManager.LoadScene("JoinRoom");
    }

    // Function to return to the main menu (optional)
    public void ReturnToMenu()
    {
        // Người chơi có thể đã bấm StartHost/StartClient rồi mới quay ra,
        // nên phải tắt phiên mạng cho sạch thay vì chỉ đổi scene.
        NetworkSession.LeaveAndLoad("MainMenu");
    }
}
