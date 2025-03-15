using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance; // Singleton instance
    public AudioMixer audioMixer; // Reference to the Audio Mixer
    public AudioSource musicSource; // Reference to the Audio Source

    private void Awake()
    {
        // Ensure only one instance of the MusicManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicates
        }

        // Ensure the AudioSource component is attached to this object
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>(); // Try to get it from the same GameObject
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>(); // Add AudioSource if it's not found
            }
        }

        // Ensure the volume is set initially
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f); // Load saved volume, default to 0.5
        SetVolume(savedVolume); // Set the volume from saved value
    }

    // Method to set master volume
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20); // Convert to decibel
        PlayerPrefs.SetFloat("MasterVolume", volume); // Save the volume for future sessions
        PlayerPrefs.Save(); // Ensure PlayerPrefs are saved
    }

    // Method to fade music (optional, for smooth transitions)
    public void FadeOutMusic(float duration)
    {
        StartCoroutine(FadeVolume(0, duration));
    }

    public void FadeInMusic(float targetVolume, float duration)
    {
        StartCoroutine(FadeVolume(targetVolume, duration));
    }

    private System.Collections.IEnumerator FadeVolume(float targetVolume, float duration)
    {
        float currentTime = 0;
        float startVolume;
        audioMixer.GetFloat("MasterVolume", out startVolume);
        startVolume = Mathf.Pow(10, startVolume / 20); // Convert back from decibel

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(newVolume) * 20);
            yield return null;
        }
    }
}
