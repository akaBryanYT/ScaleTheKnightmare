using UnityEngine;
using TMPro; // Import TextMeshPro namespace
using System.Collections.Generic;

public class ResolutionChanger : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown; // Reference to the TMP Dropdown

    private List<string> resolutionOptions = new List<string> { "1920 x 1080", "1280 x 720" }; // Preset resolutions

    void Start()
    {
        // Check if the TMP Dropdown is assigned
        if (resolutionDropdown == null)
        {
            Debug.LogError("Resolution TMP_Dropdown is not assigned!");
            return;
        }

        // Populate the TMP Dropdown with preset resolutions
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutionOptions);

        // Set listener for when the TMP Dropdown value changes
        resolutionDropdown.onValueChanged.AddListener(ChangeResolution);
    }

    // Method to change resolution based on TMP Dropdown selection
    public void ChangeResolution(int index)
    {
        Debug.Log("Selected Index: " + index); // Debug log to confirm TMP Dropdown value

        // Check the selected resolution
        switch (index)
        {
            case 0:
                // Set resolution to 1920x1080
                Screen.SetResolution(1920, 1080, Screen.fullScreen);
                break;
            case 1:
                // Set resolution to 1280x720
                Screen.SetResolution(1280, 720, Screen.fullScreen);
                break;
            default:
                break;
        }
    }
}

