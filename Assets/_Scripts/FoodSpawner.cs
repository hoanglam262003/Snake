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
        obj.GetComponent<Food>().prefab = prefab;
        if(!obj.IsSpawned) obj.Spawn(true);
    }
    private Vector3 GetRandomPositionOnMap()
    {
        return new Vector3(x: Random.Range(-9f, 9f), y: Random.Range(-5f, 5f), z: 0f); 
    }
    private IEnumerator SpawnOverTime()
    {
        while (NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            yield return new WaitForSeconds(2f);
            if (NetworkObjectPool.Singleton.GetCurrentPrefabCount(prefab) < MaxPrefabCount)
                SpawnFood();
        }
    }
}
