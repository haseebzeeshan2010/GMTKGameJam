using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;

public class SceneRestart : MonoBehaviour
{
    // This can be called from UI buttons via the inspector
    public void RestartToFirstScene()
    {
        // Start the restart coroutine
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        // If we're connected to a network session, shut it down properly
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // Register for the OnClientDisconnectCallback to know when shutdown is complete
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
            
            // Shutdown the network connection
            NetworkManager.Singleton.Shutdown();
            
            // Wait a frame to let the network shutdown process begin
            yield return null;
        }
        else
        {
            // If not connected, just load the first scene
            SceneManager.LoadScene(0);
        }
    }

    private void HandleDisconnect(ulong clientId)
    {
        // Unregister the callback to prevent memory leaks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
        }
        
        // Load the first scene in the build order (index 0)
        SceneManager.LoadScene(0);
    }
}
