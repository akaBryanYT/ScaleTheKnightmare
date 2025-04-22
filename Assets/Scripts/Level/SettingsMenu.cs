using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsPanel; // Reference to the settings panel
    public GameObject mainMenuPanel; // Reference to the main menu panel
    public GameObject controlPanel; //Reference to the control panel

    // Function to toggle the settings panel visibility
    void Start()
    {
        settingsPanel.SetActive(false);
        controlPanel.SetActive(false);
    }

    public void ToggleSettingsMenu()
    {
        bool isActive = settingsPanel.activeSelf; // Check if the settings panel is active
        settingsPanel.SetActive(!isActive); // Toggle visibility
        // Optionally, you can hide the main menu panel when settings are visible
        mainMenuPanel.SetActive(false); // Show the main menu panel if settings are hidden
    }

    public void BackButton()
    {
        bool isActive = settingsPanel.activeSelf;
        settingsPanel.SetActive(!isActive);
        mainMenuPanel.SetActive(true);
    }

    public void Controls()
    {
        bool isActive = controlPanel.activeSelf;
        controlPanel.SetActive(!isActive);
        mainMenuPanel.SetActive(false);
    }
       
    
}
