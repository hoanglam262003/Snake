using Unity.Netcode;
using UnityEngine;

public class Food : NetworkBehaviour
{
    public GameObject prefab;
    private FoodSpawner spawner;
    public void Init(FoodSpawner foodSpawner)
    {
        spawner = foodSpawner;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (!collision.CompareTag("Player")) return;
        if (!NetworkObject.IsSpawned) return;

        if (collision.TryGetComponent(out PlayerLength playerLength))
        {
            playerLength.AddLength();
        }

        if (IsSpawned)
        {
            DespawnAndNotify();
        }
    }

    private void DespawnAndNotify()
    {
        if (!NetworkObject.IsSpawned) return;

        NetworkObject.Despawn();
        spawner?.TryRespawnFood();
    }

}
