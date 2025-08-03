using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using Unity.Cinemachine;
using TMPro;

public class LeaderboardTop3 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Leaderboard leaderboard;
    [SerializeField] private Transform CameraTransform; // Reference to the camera transform

    [SerializeField] private Vector3 cameraFollowOffset = new Vector3(0, 5, -10); // Default offset values

    [SerializeField] private TextMeshProUGUI firstplace; // References to the player name UI elements
    [SerializeField] private TextMeshProUGUI secondplace;
    [SerializeField] private TextMeshProUGUI thirdplace;
    [SerializeField] private GameObject thirdplaceObject; // Reference to the third place object
    void Start()
    {
        // Make sure CameraTransform is assigned
        if (CameraTransform == null)
        {
            Debug.LogError("CameraTransform is not assigned in the LeaderboardTop3 component");
            return;
        }

        // Find all Cinemachine cameras in the scene
        var virtualCameras = FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None);
        if (virtualCameras.Length == 0)
        {
            Debug.Log("No Cinemachine cameras found in the scene");
            return;
        }

        // Set each camera's target to our CameraTransform
        foreach (var vcam in virtualCameras)
        {
            vcam.Follow = CameraTransform;
            vcam.LookAt = CameraTransform;

            // Find and configure the CinemachineFollow component
            var followComponent = vcam.GetComponent<Unity.Cinemachine.CinemachineFollow>();
            if (followComponent != null)
            {
                followComponent.FollowOffset = cameraFollowOffset;
                Debug.Log($"Set {vcam.name}'s follow offset to {cameraFollowOffset}");
            }

            Debug.Log($"Set camera {vcam.name} to follow and look at {CameraTransform.name}");
        }



        // Get the top 3 players from the leaderboard
        List<LeaderboardEntityState> top3Players = GetTop3Players();

        // if (top3Players.Count < 3)
        // {
        //     firstplace.text = top3Players[0].PlayerName.Value;
        //     secondplace.text = top3Players[1].PlayerName.Value;
        //     thirdplace.text = "";
        //     thirdplaceObject.SetActive(false);
        // }
        // else
        // {
        //     firstplace.text = top3Players[0].PlayerName.Value;
        //     secondplace.text = top3Players[1].PlayerName.Value;
        //     thirdplace.text = top3Players[2].PlayerName.Value;
        //     thirdplaceObject.SetActive(true);
        // }
        Debug.Log($"Top 3 Players Count: {top3Players.Count}");
        Debug.Log($"First Place: {top3Players.ElementAtOrDefault(0).PlayerName.Value}");
        Debug.Log($"Second Place: {top3Players.ElementAtOrDefault(1).PlayerName.Value}");
        Debug.Log($"Third Place: {top3Players.ElementAtOrDefault(2).PlayerName.Value}");
        firstplace.text = top3Players.ElementAtOrDefault(0).PlayerName.Value ?? "N/A";
        secondplace.text = top3Players.ElementAtOrDefault(1).PlayerName.Value ?? "N/A";
        thirdplace.text = top3Players.ElementAtOrDefault(2).PlayerName.Value ?? "N/A";

        // Log the results for debugging
        Debug.Log($"Updated leaderboard UI with {top3Players.Count} players");
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Gets the top 3 players from the leaderboard based on tag time (highest first)
    /// </summary>
    /// <returns>A list of LeaderboardEntityState objects representing the top 3 players</returns>
    public List<LeaderboardEntityState> GetTop3Players()
    {
        // Check if leaderboard is available
        if (leaderboard == null || leaderboard.leaderboardEntities == null)
        {
            Debug.LogWarning("Leaderboard reference is null or not initialized");
            return new List<LeaderboardEntityState>();
        }

        // Convert to List for sorting (NetworkList doesn't support LINQ directly)
        List<LeaderboardEntityState> allPlayers = new();
        foreach (var entity in leaderboard.leaderboardEntities)
        {
            allPlayers.Add(entity);
        }

        // Sort by tag time (highest first)
        List<LeaderboardEntityState> top3 = allPlayers
            .OrderByDescending(entity => entity.TagTimed)
            .Take(3)
            .ToList();

        return top3;
    }
}