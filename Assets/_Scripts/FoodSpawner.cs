using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    private const int MaxPrefabCount = 50;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnFoodStart;
    }

    private void SpawnFoodStart()
    {
        NetworkManager.Singleton.OnServerStarted -= SpawnFoodStart;
        if (!NetworkManager.Singleton.IsServer) return;

        var networkObject = NetworkObjectPool.Singleton.GetNetworkObject(prefab, Vector3.zero, Quaternion.identity);
        if (!networkObject.IsSpawned)
        {
            networkObject.Spawn(true);
        }

        for (int i = 0; i < 30; ++i)
        {
            SpawnFood();
        }

        StartCoroutine(SpawnOverTime());
    }

    private void SpawnFood()
    {
        NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(prefab, GetRandomPositionOnMap(), Quaternion.identity);
        var food = obj.GetComponent<Food>();
        food.prefab = prefab;
        food.Init(this);
        if (!obj.IsSpawned) obj.Spawn(true);
    }

    private void SpawnFoodNearAllPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            Vector3 nearPosition = GetRandomNearbyPosition(player.transform.position);
            NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(prefab, nearPosition, Quaternion.identity);
            var food = obj.GetComponent<Food>();
            food.prefab = prefab;
            food.Init(this);
            if (!obj.IsSpawned) obj.Spawn(true);
        }
    }

    private Vector3 GetRandomPositionOnMap()
    {
        return new Vector3(Random.Range(-9f, 9f), Random.Range(-5f, 5f), 0f);
    }

    private Vector3 GetRandomNearbyPosition(Vector3 center)
    {
        float offsetX = Random.Range(-5f, 5f);
        float offsetY = Random.Range(-5f, 5f);
        return new Vector3(center.x + offsetX, center.y + offsetY, 0f);
    }

    private IEnumerator SpawnOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            if (!NetworkManager.Singleton.IsServer) continue;

            int current = NetworkObjectPool.Singleton.GetCurrentPrefabCount(prefab);
            Debug.Log($"[SpawnOverTime] Current Food Count: {current}");

            if (current < MaxPrefabCount)
            {
                SpawnFood();

                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var playerObj = client.PlayerObject;
                    if (playerObj == null) continue;

                    Vector3 nearPos = GetRandomNearbyPosition(playerObj.transform.position);
                    NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(prefab, nearPos, Quaternion.identity);
                    var food = obj.GetComponent<Food>();
                    food.prefab = prefab;
                    food.Init(this);
                    if (!obj.IsSpawned) obj.Spawn(true);
                }
            }
        }
    }


    public void TryRespawnFood()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int current = NetworkObjectPool.Singleton.GetCurrentPrefabCount(prefab);
        Debug.Log($"[TryRespawnFood] Current food count: {current}");

        if (current < MaxPrefabCount)
        {
            Debug.Log("[TryRespawnFood] Respawning food after one was eaten.");
            SpawnFoodNearAllPlayers();
        }
    }
}
