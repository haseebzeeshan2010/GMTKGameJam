using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class DaylightCycle : NetworkBehaviour
{
    // Reference to the 3D light to be animated.
    [SerializeField] private Light directionalLight;

    // Animation curve for light intensity over time (0-1 represents cycle progress)
    [SerializeField] private AnimationCurve intensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.5f),  // Start with 0.5 intensity
        new Keyframe(0.5f, 1.5f), // Peak at midpoint
        new Keyframe(1f, 0.5f)   // Return to 0.5 at end
    );

    // Base and target colors.
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color targetColor = Color.yellow;

    // Base and target rotation angles (in Euler angles).
    [SerializeField] private Vector3 startRotation = new Vector3(50, -30, 0);
    [SerializeField] private Vector3 targetRotation = new Vector3(90, 0, 0);

    // Duration of the transition in seconds.
    [SerializeField] private float duration = 10f;

    public void OnUIButtonClick()
    {
        if (IsServer)
        {
            // If we're the server, directly trigger the ClientRpc
            AnimateLightClientRpc();
        }
        
    }

    [ClientRpc]
    private void AnimateLightClientRpc()
    {
        if (directionalLight != null)
        {
            StartCoroutine(AnimateLight());
        }
    }

    private IEnumerator AnimateLight()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Sample the intensity from the animation curve
            directionalLight.intensity = intensityCurve.Evaluate(t);
            
            // Interpolate color and rotation
            directionalLight.color = Color.Lerp(startColor, targetColor, t);
            directionalLight.transform.eulerAngles = Vector3.Lerp(startRotation, targetRotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the final values are set
        directionalLight.intensity = intensityCurve.Evaluate(1f);
        directionalLight.color = targetColor;
        directionalLight.transform.eulerAngles = targetRotation;
    }
}
