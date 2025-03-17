using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{

    public int rngLevel() //Select a random level to load into when hitting the play button and loading into the next level.
    {
        System.Random rnd = new System.Random();
        int level = rnd.Next(1,3);
        return level;
    }


    public void ExitGame()
    {
        Application.Quit(); // This will quit the application
        Debug.Log("Quit");
    }

    public void playButton()
    {
        int numlevel = rngLevel(); //Generate random number for level
        //int numlevel = 1; Testing purposes
        string level = $"Level{numlevel}"; //Combine Number with Level to choose the random level
        SceneManager.LoadScene(level);
    }

}