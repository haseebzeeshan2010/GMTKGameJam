using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
public class BotTarget : NetworkBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private float minX = -10f; // Minimum X coordinate
    [SerializeField] private float maxX = 10f;  // Maximum X coordinate
    [SerializeField] private float minZ = -10f; // Minimum Z coordinate
    [SerializeField] private float maxZ = 10f;  // Maximum Z coordinate
    [SerializeField] private float minDistanceToTarget = 1f; // How close before picking a new position
    [SerializeField] private float updateRate = 0.1f; // How often to update destination

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private NavMeshAgent navMeshAgent;
    private float lastUpdateTime;
    private Vector3 currentTargetPosition;
    private bool hasTarget = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Get NavMeshAgent component
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            Debug.LogError($"NavMeshAgent component missing on {gameObject.name}");
            return;
        }

        // Only the server/host should control bot movement
        if (!IsServer)
        {
            navMeshAgent.enabled = false;
            return;
        }

        // Select initial random destination
        SelectNewRandomDestination();
    }

    void FixedUpdate()
    {
        // Only update on server
        if (!IsServer || navMeshAgent == null)
            return;

        // Update at specified rate
        if (Time.time - lastUpdateTime >= updateRate)
        {
            UpdateMovementBehavior();
            lastUpdateTime = Time.time;
        }

        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
        HasAnyTaggedPlayers();
    }

    private bool HasAnyTaggedPlayers()
    {
        var players = GetConnectedPlayers();
        
        // CORE VALIDATION: Check each player's NetworkVariable tag status
        foreach (var player in players)
        {
            if (player.TagStatus.Value == Player.TagState.Tagged)
                {
                    // Found a tagged player, set destination to their position
                    hasTarget = true;
                    currentTargetPosition = player.transform.position;
                    SetDestination();
                    
                    
                }
        }
        

        // CORE EDGE CASE: Return true if no players to avoid unnecessary tagging
        return players.Count == 0;
    }

    private List<Player> GetConnectedPlayers()
    {
        var players = new List<Player>();

        // Safety check for NetworkManager
        if (NetworkManager.Singleton == null)
            return players;

        // CORE ITERATION: Use NGO 2.4.1 ConnectedClientsList for reliable client enumeration
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // CORE COMPONENT ACCESS: Proper null checking for Unity objects
            if (client.PlayerObject != null)
            {
                Player player = client.PlayerObject.GetComponent<Player>();
                if (player != null)
                {
                    players.Add(player);
                }
            }
        }

        return players;
    }

    private void UpdateMovementBehavior()
    {
        // Check if we've reached the current target
        if (hasTarget && Vector3.Distance(transform.position, currentTargetPosition) <= minDistanceToTarget)
        {
            // We're close enough, select a new destination
            SelectNewRandomDestination();
        }

        // If we don't have a valid destination, get one
        if (!hasTarget)
        {
            SelectNewRandomDestination();
        }
    }

    private void SelectNewRandomDestination()
    {
        // Get a random point within our defined boundaries
        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 randomPosition = new Vector3(randomX, transform.position.y, randomZ);

        NavMeshHit hit;
        float searchRadius = Mathf.Max(maxX - minX, maxZ - minZ); // Use the larger dimension as search radius
        if (NavMesh.SamplePosition(randomPosition, out hit, searchRadius, NavMesh.AllAreas))
        {
            currentTargetPosition = hit.position;
            hasTarget = true;
            SetDestination();
        }
        else
        {
            // Couldn't find a valid position, try again next update
            hasTarget = false;
        }
    }

    private void SetDestination()
    {
        if (navMeshAgent.isOnNavMesh && hasTarget)
        {
            navMeshAgent.SetDestination(currentTargetPosition);
        }
        else
        {
            Debug.LogWarning($"Bot {gameObject.name} is not on NavMesh or has no valid target!");
        }
    }

    private void DrawDebugInfo()
    {
        if (hasTarget)
        {
            Debug.DrawLine(transform.position, currentTargetPosition, Color.red);

            if (navMeshAgent.hasPath)
            {
                var path = navMeshAgent.path;
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.blue);
                }
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            // Draw the boundary box
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) * 0.5f, transform.position.y, (minZ + maxZ) * 0.5f);
            Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
            Gizmos.DrawWireCube(center, size);
        }
    }

    private void OnValidate()
    {
        // Ensure min values are less than max values
        if (minX > maxX)
        {
            float temp = minX;
            minX = maxX;
            maxX = temp;
        }
        
        if (minZ > maxZ)
        {
            float temp = minZ;
            minZ = maxZ;
            maxZ = temp;
        }
        
        // Clamp other values
        updateRate = Mathf.Max(0.01f, updateRate);
        minDistanceToTarget = Mathf.Max(0.1f, minDistanceToTarget);
    }
}
