using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class MasterVolume : MonoBehaviour
{
    public Slider volumeSlider;          // Reference to the slider
    public AudioMixer audioMixer;        // Reference to the Audio Mixer
    private const string volumeParameter = "MasterVolume"; // Parameter name in the Audio Mixer

    void Start()
    {
        // Initialize the slider value from saved preferences or a default value
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);  // Default volume of 0.5
        volumeSlider.value = savedVolume;  // Set slider to current volume value

        // Add listener to update volume when the slider changes
        volumeSlider.onValueChanged.AddListener(UpdateVolume);
    }

    // This method is called when the slider value changes
    void UpdateVolume(float volume)
    {
        // Set the volume on the Audio Mixer (convert volume to decibel scale)
        audioMixer.SetFloat(volumeParameter, Mathf.Log10(volume) * 20);

        // Save the volume value so it's persistent across sessions
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }
}
