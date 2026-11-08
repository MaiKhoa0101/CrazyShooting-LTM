using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject particleSystemCloud;
    public GameObject rocketSpawnerPrefab;

    public string targetSceneName = "MainGame";

    private void Start()
    {

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Thiết bị đang chạy ở chế độ Host");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Thiết bị đang chạy ở chế độ Client");
        }
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager chưa được khởi tạo hoặc không khả dụng.");
            return;
        }
        else
        {
            Debug.Log("NetworkManager bth");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    private void OnDestroy()
    {
        // BẮT BUỘC huỷ đăng ký. Nếu không, mỗi lần rời trận rồi vào lại sẽ còn
        // lại một handler của đối tượng đã bị huỷ, và lần nạp MainGame sau sẽ
        // chạy SpawnPlayer nhiều lần -> người chơi bị spawn trùng.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == targetSceneName)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("Host is auto spawn in OnSceneLoaded");
                SpawnPlayer(NetworkManager.Singleton.LocalClientId); // Host spawn player
                SpawnPrefab();
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Client {clientId} yêu cầu spawn nhân vật");
        SpawnPlayer(clientId); // Gọi spawn trên Server  
    }



    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayer(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Chỉ server mới có thể spawn đối tượng mạng.");
            return ;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab chưa được gán.");
            return;
        }


        GameObject player = Instantiate(playerPrefab);
        Debug.Log("Player được instantiate voi id: "+clientId);
        NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();


        if (playerNetworkObject == null)
        {
            Debug.LogError("Prefab của nhân vật không có thành phần NetworkObject.");
            Destroy(player);
            return;
        }

        // Chỉ server mới có thể gọi SpawnWithOwnership
        Debug.Log("player được spawn");
        playerNetworkObject.SpawnWithOwnership(clientId);

        Debug.Log($"Nhân vật đã spawn trên mạng cho client {clientId}");
    }

    private void SpawnPrefab()
    {
        if (rocketSpawnerPrefab != null)
        {
            NetworkObject rocketSpawner = Instantiate(rocketSpawnerPrefab,new Vector3(0,400,0), this.transform.rotation).GetComponent<NetworkObject>();
            rocketSpawner.Spawn(true);
        }
        else if (particleSystemCloud != null)
        {
            NetworkObject particleCloud = Instantiate(particleSystemCloud, new Vector3(335,65,0), this.transform.rotation).GetComponent<NetworkObject>();
            particleCloud.Spawn(true);
        }
    }

}