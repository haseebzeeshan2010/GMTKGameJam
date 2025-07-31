using UnityEngine;
using Unity.Netcode;

public class TeleportManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager; // Assign via Inspector, fallback to Singleton if not assigned

    // Array of teleport points visible in the inspector.
    [SerializeField] private Transform[] teleportPoints;

    void Start()
    {
        // If not assigned in the Inspector, fallback to NetworkManager.Singleton
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (networkManager != null)
        {
            // Optionally, you can teleport immediately or wait for the countdown event.
        }
        else
        {
            Debug.Log("NetworkManager is not initialized and not found.");
        }



        if (networkManager == null)
        {
            Debug.Log("NetworkManager is not available for teleportation.");
            return;
        }

        foreach (var client in networkManager.ConnectedClientsList)
        {
            // Example teleport: set player position to a random point.
            if (client.PlayerObject != null)
            {
                var playerObj = client.PlayerObject;
                var rb = playerObj.GetComponent<Rigidbody>();
                RigidbodyInterpolation prevInterpolation = RigidbodyInterpolation.None;
                if (rb != null)
                {
                    prevInterpolation = rb.interpolation;
                    rb.interpolation = RigidbodyInterpolation.None;
                }
                // Pick a random position
                Vector3 newPos = new Vector3(Random.Range(0, 10), 0, Random.Range(0, 10));
                playerObj.transform.position = newPos;
                // Ensure physics updates immediately
                Physics.SyncTransforms();
                // Restore interpolation
                if (rb != null)
                {
                    rb.interpolation = prevInterpolation;
                }
                Debug.Log($"Teleported Client ID: {client.ClientId} to new position {newPos}.");
            }
            else
            {
                Debug.Log($"Client ID: {client.ClientId} has no PlayerObject assigned.");
            }
        }
    }

    // Subscribe to the event from NetworkTimer.
    private void OnEnable()
    {
        NetworkTimer.CountdownBegan += OnCountdownBegan;
    }

    private void OnDisable()
    {
        NetworkTimer.CountdownBegan -= OnCountdownBegan;
    }

    // This method is triggered when the countdown begins.
    private void OnCountdownBegan()
    {
        TeleportPlayers();
    }

    private void TeleportPlayers()
    {
        if (networkManager == null)
        {
            Debug.Log("NetworkManager is not available for teleportation.");
            return;
        }

        // Check that teleportPoints are set in the inspector.
        if (teleportPoints == null || teleportPoints.Length == 0)
        {
            Debug.Log("No teleport points have been set in the inspector.");
            return;
        }

        foreach (var client in networkManager.ConnectedClientsList)
        {
            // Example teleport: set player position to one of the designated teleport points.
            if (client.PlayerObject != null)
            {
                var playerObj = client.PlayerObject;
                var rb = playerObj.GetComponent<Rigidbody>();
                RigidbodyInterpolation prevInterpolation = RigidbodyInterpolation.None;
                if (rb != null)
                {
                    prevInterpolation = rb.interpolation;
                    rb.interpolation = RigidbodyInterpolation.None;
                }
                // Pick a random teleport point from the array.
                int index = Random.Range(0, teleportPoints.Length);
                playerObj.transform.position = teleportPoints[index].position;
                // Ensure physics updates immediately
                Physics.SyncTransforms();
                // Restore interpolation
                if (rb != null)
                {
                    rb.interpolation = prevInterpolation;
                }
                Debug.Log($"Teleported Client ID: {client.ClientId} to teleport point [{index}] at position {teleportPoints[index].position}.");
            }
            else
            {
                Debug.Log($"Client ID: {client.ClientId} has no PlayerObject assigned.");
            }
        }
    }
}