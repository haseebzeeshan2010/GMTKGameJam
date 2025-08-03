using UnityEngine;
using System.Collections;

public class SoundScript : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeInDuration = 2.0f;
    [SerializeField] private float targetVolume = 1.0f;
    
    private bool isFadingIn = false;
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        // Get the AudioSource component if not assigned
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
            
        // Start with volume at zero
        if (musicSource != null)
            musicSource.volume = 0f;
    }
    
    void Start()
    {
        // Start fade-in when the script starts
        StartFadeIn();
    }
    
    public void StartFadeIn()
    {
        if (musicSource == null) return;
        
        // Stop any existing fade coroutine
        if (isFadingIn)
            StopAllCoroutines();
            
        // Start the fade-in coroutine
        StartCoroutine(FadeInMusic());
    }
    
    private IEnumerator FadeInMusic()
    {
        isFadingIn = true;
        
        // Play the music if it's not already playing
        if (!musicSource.isPlaying)
            musicSource.Play();
            
        float timeElapsed = 0f;
        float startVolume = musicSource.volume;
        
        while (timeElapsed < fadeInDuration)
        {
            // Calculate new volume based on time
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timeElapsed / fadeInDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the target volume
        musicSource.volume = targetVolume;
        isFadingIn = false;
    }
}
