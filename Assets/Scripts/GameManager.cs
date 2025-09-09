using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;
    public Image levelComppleteImage;

    [SerializeField] public Button nextLevelButton;
    [SerializeField] public Button restartPlayButton;
    [SerializeField] public Button quitGameButton;
    [SerializeField] public Button homeButton;
    [SerializeField] public Button pauseGameButton;
    [SerializeField] public GameObject nextAndRestartPanel;
    [SerializeField] public GameObject pauseMenuPanel;

    [SerializeField] public GameObject statusTextBackground;

    [Header("Audio Clips")]
    [SerializeField] public AudioClip cubeDragAudio;
    [SerializeField] public AudioClip stopPointAudio;
    [SerializeField] public AudioClip cubeCollectedAudio;
    [SerializeField] public AudioClip levelCompleteAudio;
    [SerializeField] public AudioClip gameOverAudio;

    [Header("AdMob")]
    [SerializeField] private AdmobInterstitial admobInterstitial;

    [Header("Level Settings")]
    [SerializeField] private int currentLevelNumber; // Set this in inspector for each level

    private AudioSource audioSource;
    private int totalCubes;
    private int cubesCollected = 0;
    private bool gameOver = false;
    private bool isWaitingForAd = false;
    private bool levelCompleted = false; // Track if level is completed

    private System.Collections.Generic.HashSet<CubeColor> collectedColors = new System.Collections.Generic.HashSet<CubeColor>();
    private CubeColor[] colorCycle = { CubeColor.Red, CubeColor.Blue, CubeColor.Green, CubeColor.Yellow };

    private bool isPaused = false;

    void Start()
    {
        // Auto-detect current level number if not set
        if (currentLevelNumber <= 0)
        {
            currentLevelNumber = GetCurrentLevelNumber();
        }

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // Find AdMob script if not assigned
        if (admobInterstitial == null)
        {
            admobInterstitial = FindFirstObjectByType<AdmobInterstitial>();
        }

        // ✅ Subscribe to AdMob events
        AdmobInterstitial.OnAdClosed += OnInterstitialAdClosed;
        AdmobInterstitial.OnAdFailedToShow += OnInterstitialAdFailed;

        SquareController[] cubes = FindObjectsOfType<SquareController>();
        totalCubes = cubes.Length;

        // Auto-assign cubes to nearest stop points
        AssignCubesToStopPoints(cubes);

        SetupButtonListeners();
        UpdateUI();
        Debug.Log($"Level {currentLevelNumber} started with {totalCubes} cubes");
    }

    /// <summary>
    /// Auto-detect current level number from scene name or build index
    /// </summary>
    private int GetCurrentLevelNumber()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        // Try to extract number from scene name first (e.g., "Level1", "Level2")
        string numberPart = "";
        foreach (char c in sceneName)
        {
            if (char.IsDigit(c))
            {
                numberPart += c;
            }
        }
        
        if (int.TryParse(numberPart, out int levelFromName))
        {
            return levelFromName;
        }
        
        // Fallback to build index (assuming first level scene is at index 1)
        return SceneManager.GetActiveScene().buildIndex;
    }

    void SetupButtonListeners()
    {
        if (restartPlayButton != null)
            restartPlayButton.onClick.AddListener(RestartLevel);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelButtonPressed);

        if (pauseGameButton != null)
            pauseGameButton.onClick.AddListener(TogglePause);

        if (homeButton != null)
            homeButton.onClick.AddListener(LoadMenu);

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    // ✅ Handle next level button press
    void OnNextLevelButtonPressed()
    {
        if (isWaitingForAd) return; // Prevent multiple button presses

        // Check if next level is unlocked
        int nextLevelNumber = currentLevelNumber + 1;
        if (!LevelManager.Instance.IsLevelUnlocked(nextLevelNumber))
        {
            Debug.LogWarning($"Next level {nextLevelNumber} is not unlocked yet!");
            return;
        }

        if (admobInterstitial != null)
        {
            isWaitingForAd = true;

            // Disable the button to prevent multiple presses
            if (nextLevelButton != null)
                nextLevelButton.interactable = false;

            Debug.Log("Showing ad before loading next level...");
            admobInterstitial.ShowInterstitialAd();

            // Set a backup timer in case something goes wrong
            StartCoroutine(AdTimeoutCoroutine());
        }
        else
        {
            Debug.LogWarning("No AdMob script found, loading next level directly");
            LoadNextLevel();
        }
    }

    // ✅ Called when ad closes successfully
    void OnInterstitialAdClosed()
    {
        if (isWaitingForAd)
        {
            Debug.Log("Ad closed, loading next level");
            isWaitingForAd = false;
            LoadNextLevel();
        }
    }

    // ✅ Called when ad fails to show
    void OnInterstitialAdFailed()
    {
        if (isWaitingForAd)
        {
            Debug.Log("Ad failed to show, loading next level anyway");
            isWaitingForAd = false;
            LoadNextLevel();
        }
    }

    // ✅ Backup timeout in case events don't fire
    System.Collections.IEnumerator AdTimeoutCoroutine()
    {
        yield return new WaitForSecondsRealtime(30f); // 30 second timeout

        if (isWaitingForAd)
        {
            Debug.LogWarning("Ad timeout reached, loading next level");
            isWaitingForAd = false;
            LoadNextLevel();
        }
    }

    void LoadNextLevel()
    {
        // Ensure we're not in a paused state
        Time.timeScale = 1f;

        int nextLevelNumber = currentLevelNumber + 1;
        
        // Try to load by level number first
        LevelManager.Instance.LoadLevel(nextLevelNumber);
    }

    // Rest of your existing methods remain the same...
    void AssignCubesToStopPoints(SquareController[] cubes)
    {
        StopPoint[] allStops = FindObjectsByType<StopPoint>(FindObjectsSortMode.None);

        foreach (SquareController cube in cubes)
        {
            if (cube.currentPoint == null)
            {
                StopPoint closest = null;
                float shortestDistance = Mathf.Infinity;

                foreach (StopPoint stop in allStops)
                {
                    if (!stop.IsOccupied())
                    {
                        float distance = Vector3.Distance(cube.transform.position, stop.transform.position);
                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            closest = stop;
                        }
                    }
                }

                if (closest != null)
                {
                    cube.currentPoint = closest;
                    closest.currentSquare = cube;
                    cube.transform.position = closest.transform.position;
                    Debug.Log($"Assigned {cube.name} to {closest.name}");
                }
            }
        }
    }

    public void CubeCollected(SquareController cube, CollectorBox collector)
    {
        if (gameOver || levelCompleted) return;

        collectedColors.Add(cube.cubeColor);
        cubesCollected++;
        Debug.Log($"Correct cube collected! ({cubesCollected}/{totalCubes}) - Color {cube.cubeColor} marked as collected");

        PlayAudio(cubeCollectedAudio);
        CleanupCubeReferences(cube);
        UpdateUI();

        if (cubesCollected >= totalCubes)
        {
            LevelComplete();
        }
    }

    public CubeColor GetNextAvailableColor(CubeColor currentColor)
    {
        SquareController[] remainingCubes = FindObjectsByType<SquareController>(FindObjectsSortMode.None);
        System.Collections.Generic.HashSet<CubeColor> availableColors = new System.Collections.Generic.HashSet<CubeColor>();

        foreach (SquareController cube in remainingCubes)
        {
            if (cube.gameObject.activeInHierarchy && !cube.isFalling)
            {
                availableColors.Add(cube.cubeColor);
            }
        }

        Debug.Log($"Available colors in scene: {string.Join(", ", availableColors)}");

        int currentIndex = System.Array.IndexOf(colorCycle, currentColor);

        for (int i = 1; i < colorCycle.Length; i++)
        {
            int nextIndex = (currentIndex + i) % colorCycle.Length;
            CubeColor nextColor = colorCycle[nextIndex];

            if (availableColors.Contains(nextColor))
            {
                Debug.Log($"Next available color: {nextColor}");
                return nextColor;
            }
        }

        Debug.Log($"No other colors available, keeping {currentColor}");
        return currentColor;
    }

    public void WrongCubeCollected(SquareController cube, CollectorBox collector)
    {
        if (gameOver || levelCompleted) return;

        Debug.Log($"GAME OVER! {cube.cubeColor} cube collected by {collector.acceptedColor} collector");
        CleanupCubeReferences(cube);
        GameOver();
    }

    public void PlayCubeDragAudio()
    {
        PlayAudio(cubeDragAudio);
    }

    public void PlayStopPointAudio()
    {
        PlayAudio(stopPointAudio);
    }

    void CleanupCubeReferences(SquareController cube)
    {
        StopPoint[] allStops = FindObjectsByType<StopPoint>(FindObjectsSortMode.None);

        foreach (StopPoint stop in allStops)
        {
            if (stop.currentSquare == cube)
            {
                Debug.Log($"Cleaning up reference to {cube.name} from {stop.name}");
                stop.currentSquare = null;
            }
        }
    }

    void UpdateUI()
    {
        if (statusText != null)
        {
            if (gameOver)
            {
                statusText.text = "GAME OVER!";
                statusText.color = Color.red;
            }
            else if (levelCompleted)
            {
                statusText.text = $"Level {currentLevelNumber} Complete!";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = $"Cubes: {cubesCollected}";
            }
        }
    }

    void LevelComplete()
    {
        if (levelCompleted) return; // Prevent multiple calls
        
        levelCompleted = true;
        
        // Mark this level as completed and unlock next level
        LevelManager.Instance.CompleteLevel(currentLevelNumber);
        
        if (levelComppleteImage != null)
            levelComppleteImage.gameObject.SetActive(true);

        if (statusText != null)
            statusText.gameObject.SetActive(false);

        if (statusTextBackground != null)
            statusTextBackground.gameObject.SetActive(false);

        if (nextAndRestartPanel != null)
            nextAndRestartPanel.gameObject.SetActive(true);

        if (pauseGameButton != null)
            pauseGameButton.gameObject.SetActive(false);

        // Check if next level exists and update next button
        int nextLevelNumber = currentLevelNumber + 1;
        if (nextLevelButton != null)
        {
            // Hide next button if this is the last level
            if (nextLevelNumber > LevelManager.Instance.totalLevels)
            {
                nextLevelButton.gameObject.SetActive(false);
            }
        }

        PlayAudio(levelCompleteAudio);
        Debug.Log($"Level {currentLevelNumber} Complete! Next level {nextLevelNumber} unlocked.");
    }

    void GameOver()
    {
        gameOver = true;
        PlayAudio(gameOverAudio);
        UpdateUI();

        InputManager inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager != null)
        {
            inputManager.enabled = false;
        }

        Debug.Log("Game Over! Wrong color cube was collected.");
    }

    void PlayAudio(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log(isPaused ? "Game Paused" : "Game Resumed");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game Pressed");
        Application.Quit();
    }

    // ✅ Clean up event subscriptions when destroyed
    void OnDestroy()
    {
        AdmobInterstitial.OnAdClosed -= OnInterstitialAdClosed;
        AdmobInterstitial.OnAdFailedToShow -= OnInterstitialAdFailed;
    }
}