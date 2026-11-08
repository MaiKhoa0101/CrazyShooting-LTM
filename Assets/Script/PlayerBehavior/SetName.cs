using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SetName : NetworkBehaviour
{
    // NetworkVariable to store the player's name  
    private NetworkVariable<FixedString128Bytes> playerNameNetVariable = new NetworkVariable<FixedString128Bytes>();
    public TMP_Text nameDisplay;

    /// <summary>
    /// Tên người chơi đã đồng bộ qua mạng. Trả về chuỗi rỗng nếu chưa kịp gán.
    /// MatchManager đọc giá trị này trên server để xác định tên người thắng.
    /// </summary>
    public string PlayerName => playerNameNetVariable.Value.ToString();

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            Debug.Log("This is host");
        }
        else if (IsClient)
        {
            Debug.Log("This is Client");
            // Cập nhật nameDisplay cho máy khách  
            UpdateNameDisplay(playerNameNetVariable.Value.ToString());
        }

        Debug.Log("Ready to enter setname");

        // Thay đổi 'SetCharacter.playerNameText' nếu cần kiểm tra độ hợp lệ  
        if (IsOwner)
        {
            Debug.Log("Owner check pass");
            SetPlayerNameServerRpc(SetCharacter.playerNameText);
        }
    }

    // Server RPC to set the player name  
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        Debug.Log("Player is on set name: playerName:" + playerName);
        // Set the NetworkVariable on the server  
        playerNameNetVariable.Value = playerName;

        // Gọi ClientRpc để cập nhật nameDisplay cho tất cả clients  
        UpdateNameDisplayClientRpc(playerName);
        Debug.Log("SetPlayerNameServerRpc-----> playerName:" + playerName + "\n playerNameNetVariable.Value: " + playerNameNetVariable.Value);
    }

    // ClientRpc để cập nhật nameDisplay  
    [ClientRpc]
    private void UpdateNameDisplayClientRpc(string playerName)
    {
        nameDisplay.text = playerName; // Cập nhật Giao diện cho Client  
        Debug.Log("Updated nameDisplay for player: " + playerName);
    }

    // Phương thức này sẽ cập nhật chế độ hiển thị cho máy khách  
    private void UpdateNameDisplay(string playerName)
    {
        nameDisplay.text = playerName;
        Debug.Log("UpdateNameDisplay: "+nameDisplay);
    }
}