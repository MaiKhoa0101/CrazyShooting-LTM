using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField]
    private GameObject[] playerPrefabs; // Array of player prefabs

    private NetworkVariable<FixedString128Bytes> playerCharNetVariable = new NetworkVariable<FixedString128Bytes>();

    // Called when the object is spawned on the network
    public override void OnNetworkSpawn()
    {

        if (IsClient)
        {
            Debug.Log("This is Client On PlayerManager");
            SetPlayerCharacterServerRpc(SetCharacter.selectedCharacterSprite);
        }
        
    }

    // Server RPC to set the player character on the server
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerCharacterServerRpc(string playerChar, ServerRpcParams rpcParams = default)
    {
        Debug.Log("Setting character on server: " + playerChar);
        playerCharNetVariable.Value = playerChar; // Store the character selection in the network variable

        // Spawn luôn tại đây, trên server.
        SpawnPlayerForClient(playerChar, rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Tạo nhân vật và giao quyền sở hữu cho client đã yêu cầu. CHỈ chạy trên server.
    ///
    /// Trước đây phần này nằm trong một [ClientRpc] tên SpawnPlayerClientRpc, mà
    /// bên trong lại chặn "if (!IsHost) return;" rồi mới gọi SpawnAsPlayerObject.
    /// Tức là: phát một lệnh đi khắp mọi máy, rồi bảo tất cả trừ host bỏ qua.
    /// Logic ngược — gửi thừa cho mọi client, và che giấu việc spawn vốn dĩ là
    /// việc riêng của server. Nay gọi thẳng trong ServerRpc.
    /// </summary>
    private void SpawnPlayerForClient(string selectedCharacter, ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        Debug.Log("SpawnPlayerForClient, ID: " + clientId);
        if (playerPrefabs == null || playerPrefabs.Length < 4)
        {
            Debug.LogError("[PlayerManager] Mảng playerPrefabs phải có đủ 4 nhân vật.");
            return;
        }

        GameObject playerInstance;
        NetworkObject networkObject;

        // Instantiate the correct player prefab based on selected character
        Debug.Log("Selected Character: " + selectedCharacter);

        if (selectedCharacter == "Assassin")
        {
            playerInstance = Instantiate(playerPrefabs[0], transform.position, Quaternion.identity);
            Debug.Log("Client spawned Assassin");
        }
        else if (selectedCharacter == "Cowboy")
        {
            playerInstance = Instantiate(playerPrefabs[1], transform.position, Quaternion.identity);
            Debug.Log("Client spawned Cowboy");
        }
        else if (selectedCharacter == "Madman")
        {
            playerInstance = Instantiate(playerPrefabs[2], transform.position, Quaternion.identity);
            Debug.Log("Client spawned Madman");
        }
        else
        {
            playerInstance = Instantiate(playerPrefabs[3], transform.position, Quaternion.identity);
            Debug.Log("Client spawned Robot");
        }

        Debug.Log("SPAWNNNNNNNNNNNNNNNNNNN");

        networkObject = playerInstance.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            // Spawn the player object and assign ownership to the specific client ID
            networkObject.SpawnAsPlayerObject(clientId);
            Debug.Log("Player spawned for client ID: " + clientId);
        }
        else
        {
            Debug.LogError("The instantiated player prefab does not have a NetworkObject component.");
            Destroy(playerInstance);
        }
    }
}
