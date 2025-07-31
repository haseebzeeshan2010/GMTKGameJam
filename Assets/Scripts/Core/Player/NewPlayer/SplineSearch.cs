using UnityEngine;
using Unity.Cinemachine;

public class SplineDollyAssigner : MonoBehaviour
{
    // Assign the Cinemachine Spline Dolly in the Inspector.
    [SerializeField] private CinemachineSplineDolly splineDolly;
    // Optionally assign a CinemachineSmoothPath (spline) in the Inspector.
    // If left unassigned, the script will search for a SplineContainer in the scene.
    [SerializeField] private UnityEngine.Splines.SplineContainer splineContainer;


    private void Update()
    {
        // If no SplineContainer is set via the Inspector, search the scene
        if (splineContainer == null)
        {
            splineContainer = FindFirstObjectByType<UnityEngine.Splines.SplineContainer>();
            
            if (splineContainer == null)
            {
                Debug.LogWarning("No SplineContainer found in the scene.");
            }
            else
            {
                // Optionally, link the found SplineContainer to the Spline Dolly.
                // (Ensure CinemachineSplineDolly is set up to work with a SplineContainer)
                splineDolly.Spline = splineContainer;
            }
        }
    }
}