using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening; // Add DOTween reference

public class LeaderboardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text UsernameText;
    [SerializeField] private TMP_Text ScoreText;
    [SerializeField] private Color myColour;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Ease fadeOutEase = Ease.InQuart;
    [SerializeField] private float scoreAnimDuration = 0.15f;
    [SerializeField] private float scoreBounceMagnitude = 3f;
    [SerializeField] private Color highlightColor = Color.yellow;

    private FixedString32Bytes playerName;
    private Coroutine connectionCheckCoroutine;
    private CanvasGroup canvasGroup;
    private bool isBeingDestroyed = false;
    
    // Store active tweens to manage them properly
    private Sequence activeScoreSequence;

    public static event Action<ulong> OnPlayerDisconnected;

    public ulong ClientId { get; private set; }
    public int TagTimed { get; private set; }

    private void Awake()
    {
        // Subscribe to disconnection events
        OnPlayerDisconnected += HandlePlayerDisconnected;
        
        // Ensure we have a CanvasGroup for fade animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Start with full alpha
        canvasGroup.alpha = 1f;
    }

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int tagTimes)
    {
        ClientId = clientId;
        this.playerName = playerName;
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UsernameText.color = myColour;
        }
        
        // Set initial values without animation
        TagTimed = tagTimes;
        UpdateText();
        
        // Start checking player connection status (only on server/host)
        if (NetworkManager.Singleton.IsServer)
        {
            connectionCheckCoroutine = StartCoroutine(CheckPlayerConnection());
        }
    }

    private IEnumerator CheckPlayerConnection()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            if (ClientId == 100) yield break; // Skip check for bots
            // Check if NetworkManager is still valid and if client is still connected
            if (NetworkManager.Singleton != null && 
                !NetworkManager.Singleton.ConnectedClients.ContainsKey(ClientId))
            {
                // Player disconnected - trigger event for all local instances
                OnPlayerDisconnected?.Invoke(ClientId);
                yield break; // Exit the coroutine
            }
        }
    }

    private void HandlePlayerDisconnected(ulong disconnectedClientId)
    {
        if (ClientId == disconnectedClientId && !isBeingDestroyed)
        {
            // This leaderboard entry belongs to the disconnected player
            StartFadeOutAndDestroy();
        }
    }

    private void StartFadeOutAndDestroy()
    {
        isBeingDestroyed = true;
        
        // Stop the connection check coroutine if it's running
        if (connectionCheckCoroutine != null)
        {
            StopCoroutine(connectionCheckCoroutine);
            connectionCheckCoroutine = null;
        }

        // Kill any active animations first
        KillActiveAnimations();

        // Animate fade out with CanvasGroup alpha
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(fadeOutEase)
                .OnComplete(() =>
                {
                    // Add a small delay before destroying the GameObject
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        if (gameObject != null)
                        {
                            Destroy(gameObject);
                        }
                    });
                });
        }
    }

    public void UpdateTagTime(int tagTimes)
    {
        // Don't update if we're being destroyed
        if (isBeingDestroyed) return;
        
        // Skip animation if the value hasn't changed
        if (TagTimed == tagTimes) return;
        
        // Update data and text first
        TagTimed = tagTimes;
        UpdateText();
        
        // Check if this GameObject is actively moving (part of leaderboard reordering)
        bool isCurrentlyMoving = DOTween.IsTweening(transform);
        
        // Kill any active animation to prevent conflicts
        KillActiveAnimations();
        
        // If we're already being animated by the leaderboard, use a simpler animation
        if (isCurrentlyMoving)
        {
            // Just do a quick color flash without position change
            if (ScoreText != null)
            {
                Color originalColor = ScoreText.color;
                
                activeScoreSequence = DOTween.Sequence();
                activeScoreSequence.Append(ScoreText.DOColor(highlightColor, scoreAnimDuration));
                activeScoreSequence.Append(ScoreText.DOColor(originalColor, scoreAnimDuration));
                
                // Force completion if interrupted
                activeScoreSequence.OnKill(() => {
                    if (ScoreText != null) {
                        ScoreText.color = originalColor;
                    }
                });
            }
            return;
        }
        
        // Animate score change with a color flash and vertical bounce
        if (ScoreText != null)
        {
            // Store original values
            Color originalColor = ScoreText.color;
            Vector3 originalPosition = ScoreText.transform.localPosition;
            Vector3 bounceOffset = new Vector3(0, scoreBounceMagnitude, 0); // Vertical offset only
            
            // Create and store animation sequence
            activeScoreSequence = DOTween.Sequence();
            
            // Add color pulse (to highlight)
            activeScoreSequence.Append(ScoreText.DOColor(highlightColor, scoreAnimDuration));
            activeScoreSequence.Append(ScoreText.DOColor(originalColor, scoreAnimDuration));
            
            // Add vertical bounce that won't affect width
            activeScoreSequence.Join(
                DOTween.Sequence()
                    .Append(ScoreText.transform.DOLocalMove(originalPosition + bounceOffset, scoreAnimDuration))
                    .Append(ScoreText.transform.DOLocalMove(originalPosition, scoreAnimDuration))
            );
            
            // Ensure we handle interruptions by setting original values
            activeScoreSequence.OnKill(() => {
                if (ScoreText != null) {
                    ScoreText.transform.localPosition = originalPosition;
                    ScoreText.color = originalColor;
                }
            });
        }
    }

    public void UpdateText()
    {
        // Don't update text if we're being destroyed
        if (isBeingDestroyed) return;

        // Check if playerName is empty or null and display "HydraBot" in that case
        UsernameText.text = string.IsNullOrEmpty(playerName.ToString()) ? "HydraBot" : $"{playerName}";
        ScoreText.text = $"{TagTimed}s"; // Add 's' for seconds
    }
    
    private void KillActiveAnimations()
    {
        // Kill active score animation if it exists
        if (activeScoreSequence != null && activeScoreSequence.IsActive())
        {
            activeScoreSequence.Kill(false); // Don't complete the tween, just kill it
            activeScoreSequence = null;
        }
        
        // Kill any direct tweens on the ScoreText
        if (ScoreText != null)
        {
            DOTween.Kill(ScoreText);
            DOTween.Kill(ScoreText.transform);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        OnPlayerDisconnected -= HandlePlayerDisconnected;
        
        // Stop the connection check coroutine if it's running
        if (connectionCheckCoroutine != null)
        {
            StopCoroutine(connectionCheckCoroutine);
        }
        
        // Kill any running tweens
        KillActiveAnimations();
        transform.DOKill();
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }
}