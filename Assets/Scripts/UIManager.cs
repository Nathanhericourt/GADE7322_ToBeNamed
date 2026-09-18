using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [Tooltip("Players current resource total displayed in the UI")]
    public Text resourceText;

    [Tooltip("Towers current health displayed in the UI")]
    public Text towerHealthText;

    [Header("Game Over")]
    [Tooltip("The Game Over UI panel")]
    public GameObject gameOverPanel;

    [Header("Pause")]
    [Tooltip("Panel shown while the game is paused")]
    public GameObject pausePanel;

    private Tower tower;
    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        tower = GetComponent<Tower>();

        if (tower == null)
        {
            Debug.LogError("No Tower found in the scene.");
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (resourceText != null && ResourceManager.Instance != null)
        {
            resourceText.text = "Resources: " + ResourceManager.Instance.currentResource;
        }

        if (towerHealthText != null && tower != null)
        {
            towerHealthText.text = "Tower Health: " + tower.CurrentHealth + " / " + tower.MaxHealth;
        }
    }

    //Called when towers health reaches 0
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f; // Pause the game
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        Time.timeScale = isPaused ? 0f : 1f; // Pause or resume the game
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
