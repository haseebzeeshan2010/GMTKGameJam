using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

public class SplineCamera : MonoBehaviour
{
    [Header("Spline Settings")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform target;
    
    [Header("Movement Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float lookAtDamping = 2f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -5);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    private float currentSplineTime = 0f;
    private float splineLength;
    
    void Start()
    {
        if (splineContainer == null || target == null)
        {
            Debug.LogError("SplineCamera: SplineContainer or Target not assigned!");
            enabled = false;
            return;
        }
        
        // Calculate total spline length for constant speed movement
        splineLength = SplineUtility.CalculateLength(splineContainer.Spline, splineContainer.transform.localToWorldMatrix);
        
        // Position camera at start of spline
        Vector3 initialPosition = SplineUtility.EvaluatePosition(splineContainer.Spline, 0f);
        transform.position = splineContainer.transform.TransformPoint(initialPosition) + cameraOffset;
    }

    void Update()
    {
        if (splineContainer == null || target == null) return;
        
        UpdateCameraPosition();
        UpdateCameraRotation();
    }
    
    private void UpdateCameraPosition()
    {
        // Find the closest point on spline to target
        float targetSplineTime = FindClosestPointOnSpline(target.position);
        
        // Smoothly move current spline time toward target time
        currentSplineTime = Mathf.MoveTowards(currentSplineTime, targetSplineTime, 
            (followSpeed / splineLength) * Time.deltaTime);
        
        // Clamp to spline bounds
        currentSplineTime = Mathf.Clamp01(currentSplineTime);
        
        // Get position on spline and apply offset
        Vector3 splinePosition = SplineUtility.EvaluatePosition(splineContainer.Spline, currentSplineTime);
        Vector3 worldSplinePosition = splineContainer.transform.TransformPoint(splinePosition);
        
        // Apply camera offset relative to spline direction
        Vector3 splineDirection = GetSplineDirection(currentSplineTime);
        Vector3 rightVector = Vector3.Cross(Vector3.up, splineDirection).normalized;
        Vector3 upVector = Vector3.Cross(splineDirection, rightVector).normalized;
        
        Vector3 finalOffset = rightVector * cameraOffset.x + 
                             upVector * cameraOffset.y + 
                             splineDirection * cameraOffset.z;
        
        transform.position = worldSplinePosition + finalOffset;
    }
    
    private void UpdateCameraRotation()
    {
        // Look at target with smooth damping
        Vector3 lookDirection = (target.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
            lookAtDamping * Time.deltaTime);
    }
    
    private float FindClosestPointOnSpline(Vector3 worldPosition)
    {
        Vector3 localPosition = splineContainer.transform.InverseTransformPoint(worldPosition);
        
        float closestTime = 0f;
        float closestDistance = float.MaxValue;
        
        // Sample spline at regular intervals to find closest point
        int sampleCount = 100;
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector3 splinePoint = SplineUtility.EvaluatePosition(splineContainer.Spline, t);
            float distance = Vector3.Distance(localPosition, splinePoint);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTime = t;
            }
        }
        
        // Refine the result with a smaller search around the closest point
        float refinementRange = 1f / sampleCount;
        int refinementSamples = 20;
        
        for (int i = 0; i <= refinementSamples; i++)
        {
            float t = Mathf.Clamp01(closestTime - refinementRange + 
                (2f * refinementRange * i / refinementSamples));
            Vector3 splinePoint = SplineUtility.EvaluatePosition(splineContainer.Spline, t);
            float distance = Vector3.Distance(localPosition, splinePoint);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTime = t;
            }
        }
        
        return closestTime;
    }
    
    private Vector3 GetSplineDirection(float t)
    {
        Vector3 tangent = SplineUtility.EvaluateTangent(splineContainer.Spline, t);
        return splineContainer.transform.TransformDirection(tangent).normalized;
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || splineContainer == null) return;
        
        // Draw current position on spline
        Gizmos.color = Color.green;
        Vector3 currentPos = SplineUtility.EvaluatePosition(splineContainer.Spline, currentSplineTime);
        Vector3 worldCurrentPos = splineContainer.transform.TransformPoint(currentPos);
        Gizmos.DrawWireSphere(worldCurrentPos, 0.5f);
        
        // Draw connection to target
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
        
        // Draw camera offset visualization
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
    }
}