using System.Collections;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 3f;

    [CanBeNull]public static event System.Action GameOverEvent;

    private Camera _mainCamera;
    private Vector3 _mouseInput = Vector3.zero;
    private PlayerLength _playerLength;
    private bool _canCollide = true;

    private readonly ulong[] _targetClientsArray = new ulong[1];

    private void Initialize()
    {
        _mainCamera = Camera.main;
        _playerLength = GetComponent<PlayerLength>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Update()
    {
        if (!IsOwner || !Application.isFocused) return;
        MovePlayerServer();
    }

    private void MovePlayerServer()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        _mouseInput = new Vector3(mousePosition.x, mousePosition.y, _mainCamera.nearClipPlane);
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(_mouseInput);
        mouseWorldCoordinates.z = 0f;
        MovePlayerServerRpc(mouseWorldCoordinates);
    }

    [ServerRpc]
    private void MovePlayerServerRpc(Vector3 mouseWorldCoordinates)
    {
        transform.position = Vector3.MoveTowards(current: transform.position, target: mouseWorldCoordinates, Time.deltaTime * speed);

        //Rotation
        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }

    private void MovePlayerClient()
    {
        //Movement
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        _mouseInput = new Vector3(mousePosition.x, mousePosition.y, _mainCamera.nearClipPlane);
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(_mouseInput);
        mouseWorldCoordinates.z = 0f;
        transform.position = Vector3.MoveTowards(current: transform.position, target: mouseWorldCoordinates, Time.deltaTime * speed);

        //Rotation
        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DetermineCollisionWinnerServerRpc(PlayerData player1, PlayerData player2)
    {
        if (player1.length > player2.length)
        {
            WinInformationServerRpc(player1.id, player2.id);
        }
        else
        {
            WinInformationServerRpc(player2.id, player1.id);
        }
    }

    [ServerRpc]
    private void WinInformationServerRpc(ulong winner, ulong loser) 
    {
        _targetClientsArray[0] = winner;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = _targetClientsArray
            }
        };
        AtePlayerClientRpc();
        _targetClientsArray[0] = loser;
        clientRpcParams.Send.TargetClientIds = _targetClientsArray;
        GameOverClientRpc();
    }

    [ClientRpc]
    private void AtePlayerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        Debug.Log("You Ate a Player!!!!");
    }

    [ClientRpc]
    private void GameOverClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        Debug.Log("You Lose!!!!");
        GameOverEvent?.Invoke();
        NetworkManager.Singleton.Shutdown();
    }

    private IEnumerator CollisionCheckCoroutine()
    {
        _canCollide = false;
        yield return new WaitForSeconds(0.5f);
        _canCollide = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Player Collision");
        if (!collision.gameObject.CompareTag("Player")) return;
        if (!IsOwner) return;
        if (!_canCollide) return;
        StartCoroutine(CollisionCheckCoroutine());

        if (collision.gameObject.TryGetComponent(out PlayerLength playerLength))
        {
            Debug.Log("Head Collision");
            var player1 = new PlayerData
            {
                id = OwnerClientId,
                length = _playerLength.length.Value
            };
            var player2 = new PlayerData
            {
                id = playerLength.OwnerClientId,
                length = _playerLength.length.Value
            };
            DetermineCollisionWinnerServerRpc(player1, player2);
        }
        else if (collision.gameObject.TryGetComponent(out Tail tail))
        {
            Debug.Log("Tail Collision");
            WinInformationServerRpc(tail.networkedOwner.GetComponent<PlayerController>().OwnerClientId, OwnerClientId);
        }
    }

    struct PlayerData : INetworkSerializable
    {
        public ulong id;
        public ushort length;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref length);
        }
    }
}