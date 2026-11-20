using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Type the EXACT name of your gameplay scene here (e.g., PartOne_Main_Scene...)")]
    public string gameplaySceneName = "PartOne_Main_Scene"; 

    public void PlayGame()
    {
        Time.timeScale = 1f; 
        
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            QuitGame();
        }
    }
}