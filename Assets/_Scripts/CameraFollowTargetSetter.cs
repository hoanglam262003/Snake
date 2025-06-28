using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class CameraFollowTargetSetter : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cineCam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (cineCam == null)
            cineCam = FindFirstObjectByType<CinemachineCamera>();

        if (cineCam != null)
        {
            cineCam.Follow = transform;
        }
        else
        {
            Debug.LogWarning("CinemachineCamera not found in the scene.");
        }
    }
}
