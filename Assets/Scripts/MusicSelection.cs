using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicSelection : MonoBehaviour
{
    public AudioClip[] musicTracks; // Drag your music clips here in the Inspector
    public AudioMixerGroup musicMixerGroup; // Assign the Music group from the Audio Mixer
    private AudioSource audioSource;

    void Awake()
    {
        // Ensure the music manager persists across scenes
        if (transform.parent == null) // Check if the GameObject is a root object
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DontDestroyOnLoad(transform.root.gameObject); // Make parent persistent if nested
        }

        // Check if AudioSource is attached, and if not, add it
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Assign the AudioMixer group to the AudioSource
        audioSource.outputAudioMixerGroup = musicMixerGroup;

        // Ensure the AudioSource is enabled
        audioSource.enabled = true;

        // Subscribe to the scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Play music based on the loaded scene
        PlayMusicForScene(scene.name);
    }

    void PlayMusicForScene(string sceneName)
    {
        // Stop any previous music before switching
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Assign music based on the current scene
        switch (sceneName)
        {
            case "Main Menu":
                audioSource.clip = musicTracks[0]; // Main menu music track
                break;
            default:
                // Randomly pick a track from the rest of the array (excluding index 0)
                int index = Random.Range(1, musicTracks.Length); // Using Unity's Random.Range
                audioSource.clip = musicTracks[index];
                break;
        }

        audioSource.Play(); // Play the selected music track
    }

    // Unsubscribe from the sceneLoaded event when the object is destroyed
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
