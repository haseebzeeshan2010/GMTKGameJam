using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class FadeUI : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Startup Behavior")]
    [SerializeField] private bool fadeInOnAwake = false;
    [SerializeField] private bool fadeOutOnAwake = false;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onFadeInComplete;
    [SerializeField] private UnityEvent onFadeOutComplete;
    
    private CanvasGroup canvasGroup;
    private Tween currentTween;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    
    private void Start()
    {
        // Apply initial fade state if requested
        if (fadeInOnAwake)
        {
            // Start invisible and fade in
            canvasGroup.alpha = 0f;
            FadeIn();
        }
        else if (fadeOutOnAwake)
        {
            // Start visible and fade out
            canvasGroup.alpha = 1f;
            FadeOut();
        }
    }
    
    /// <summary>
    /// Fades the UI element in or out
    /// </summary>
    /// <param name="fadeIn">True to fade in, false to fade out</param>
    public void Fade(bool fadeIn)
    {
        // Kill any existing tween to prevent conflicts
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
        
        float targetAlpha = fadeIn ? 1f : 0f;
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        AnimationCurve curve = fadeIn ? fadeInCurve : fadeOutCurve;
        
        // Configure the UI element based on fade direction
        if (fadeIn)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        
        // Create the tween
        currentTween = canvasGroup.DOFade(targetAlpha, duration)
            .SetEase(curve)
            .OnComplete(() => {
                if (!fadeIn)
                {
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    onFadeOutComplete?.Invoke();
                }
                else
                {
                    onFadeInComplete?.Invoke();
                }
            });
    }
    
    /// <summary>
    /// Convenience method to fade in
    /// </summary>
    public void FadeIn()
    {
        Fade(true);
    }
    
    /// <summary>
    /// Convenience method to fade out
    /// </summary>
    public void FadeOut()
    {
        Fade(false);
    }
    
    /// <summary>
    /// Toggles the fade state. If currently visible, fades out. If currently hidden, fades in.
    /// </summary>
    public void ToggleFade()
    {
        // Determine the current visibility state and toggle it
        bool isCurrentlyVisible = canvasGroup.alpha > 0.5f;
        Fade(!isCurrentlyVisible);
    }
    
    private void OnDestroy()
    {
        // Clean up any active tweens when the object is destroyed
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }
}
