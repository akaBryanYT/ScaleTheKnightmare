using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{

    public int rngLevel() //Select a random level to load into when hitting the play button and loading into the next level.
    {
        System.Random rnd = new System.Random();
        int level = rnd.Next(1, 4);
        return level;
    }


    public void ExitGame()
    {
        Application.Quit(); // This will quit the application
        Debug.Log("Quit");
    }

    public void MainMenu() //Main Menu button in pause menu will change scene to the main menu
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void playButton()
    {
        int numlevel = rngLevel(); //Generate random number for level
        string level = $"Level{numlevel}"; //Combine Number with Level to choose the random level
        SceneManager.LoadScene(level);
    }

}