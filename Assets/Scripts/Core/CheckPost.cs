using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class CheckPost : NetworkBehaviour
{
    [Header("Checkpoint Data")]
    public NetworkVariable<FixedString32Bytes> FirstPlayerName = new NetworkVariable<FixedString32Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        // Only server handles checkpoint logic
        if (!IsHost) return;
        Debug.Log($"Checkpoint triggered by: {other.gameObject.name}");
        // Check if we already have a player name stored (first player only)
        if (!FirstPlayerName.Value.IsEmpty) return;

        // Try to get the Player component from the colliding object
        Player player = null;

        // Check if the collider itself has a Player component
        if (other.TryGetComponent<Player>(out player))
        {
            // Store the first player's name
            FirstPlayerName.Value = player.PlayerName.Value;
            Debug.Log($"Checkpoint reached by first player: {player.PlayerName.Value}");
        }
        // If not, check the attached rigidbody (common pattern for compound colliders)
        else if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<Player>(out player))
        {
            // Store the first player's name
            FirstPlayerName.Value = player.PlayerName.Value;
            Debug.Log($"Checkpoint reached by first player: {player.PlayerName.Value}");
        }
    }
}

//NOTE: possible improvement to use rpcs occasionally rather than network variable