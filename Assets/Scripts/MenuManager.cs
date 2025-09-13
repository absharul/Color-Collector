using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button selectLevelButton;

    [Header("UI Elements (Optional)")]
    [SerializeField] private TextMeshProUGUI startButtonText; // Optional: To show "Continue Level X"
    [SerializeField] private TextMeshProUGUI progressText; // Optional: To show overall progress

    [Header("Banner Ad Integration")]
    [SerializeField] private BannerAd bannerAdManager; // Reference to BannerAdManager GameObject
    [SerializeField] private bool showBannerOnMenuLoad = true; // Auto-show banner when menu loads
    [SerializeField] private bool hideBannerOnMenuExit = true; // Auto-hide banner when leaving menu

    [Header("Scene Loading")]
    [SerializeField] private string levelMenuScene = "LevelMenu";
    [SerializeField] private int fallbackBuildIndex = 1;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip buttonClickSound;
    private AudioSource audioSource;

    private void Awake()
    {
        EnsureEventSystem();
        SetupAudio();
        WireButtons();
        ValidateScene();
        UpdateUI();
        
        // Find BannerAd if not assigned
        if (bannerAdManager == null)
        {
            bannerAdManager = FindFirstObjectByType<BannerAd>();
        }
    }

    private void Start()
    {
        // Update UI after LevelManager is fully initialized
        UpdateUI();
        
        // Show banner ad if enabled and available
        if (showBannerOnMenuLoad && bannerAdManager != null)
        {
            ShowBannerAd();
        }
        
        // Subscribe to banner ad events
        SubscribeToBannerEvents();
    }

    private void SubscribeToBannerEvents()
    {
        // Subscribe to banner ad events for additional handling if needed
        BannerAd.OnBannerLoaded += OnBannerAdLoaded;
        BannerAd.OnBannerLoadFailed += OnBannerAdLoadFailed;
        BannerAd.OnBannerClicked += OnBannerAdClicked;
    }

    private void UnsubscribeFromBannerEvents()
    {
        // Unsubscribe from events to prevent memory leaks
        BannerAd.OnBannerLoaded -= OnBannerAdLoaded;
        BannerAd.OnBannerLoadFailed -= OnBannerAdLoadFailed;
        BannerAd.OnBannerClicked -= OnBannerAdClicked;
    }

    #region Banner Ad Event Handlers

    private void OnBannerAdLoaded()
    {
        Debug.Log("[MenuManager] Banner ad loaded in menu");
        // You can add additional logic here when banner loads
        // For example: adjust UI, show success message, etc.
    }

    private void OnBannerAdLoadFailed(string errorMessage)
    {
        Debug.LogWarning($"[MenuManager] Banner ad failed to load in menu: {errorMessage}");
        // You can add fallback logic here
        // For example: show alternative content, retry later, etc.
    }

    private void OnBannerAdClicked()
    {
        Debug.Log("[MenuManager] Banner ad clicked in menu");
        // You can add analytics tracking or other logic here
    }

    #endregion

    #region Banner Ad Control Methods

    /// <summary>
    /// Show the banner ad
    /// </summary>
    public void ShowBannerAd()
    {
        if (bannerAdManager != null)
        {
            bannerAdManager.ShowBannerAd();
            Debug.Log("[MenuManager] Requesting to show banner ad");
        }
        else
        {
            Debug.LogWarning("[MenuManager] BannerAd manager not found! Make sure BannerAdManager GameObject exists in scene.");
        }
    }

    /// <summary>
    /// Hide the banner ad
    /// </summary>
    public void HideBannerAd()
    {
        if (bannerAdManager != null)
        {
            bannerAdManager.HideBannerAd();
            Debug.Log("[MenuManager] Requesting to hide banner ad");
        }
    }

    /// <summary>
    /// Toggle banner ad visibility
    /// </summary>
    public void ToggleBannerAd()
    {
        if (bannerAdManager != null)
        {
            if (bannerAdManager.IsBannerShowing())
            {
                HideBannerAd();
            }
            else
            {
                ShowBannerAd();
            }
        }
    }

    /// <summary>
    /// Check if banner ad is currently showing
    /// </summary>
    public bool IsBannerAdShowing()
    {
        return bannerAdManager != null && bannerAdManager.IsBannerShowing();
    }

    #endregion

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && buttonClickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void WireButtons()
    {
        if (startButton == null) Debug.LogError("Start Button is not assigned on MenuManager.");
        if (quitButton == null) Debug.LogError("Quit Button is not assigned on MenuManager.");
        if (selectLevelButton == null) Debug.LogError("Select Level Button is not assigned on MenuManager.");

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }

        if (selectLevelButton != null)
        {
            selectLevelButton.onClick.RemoveAllListeners();
            selectLevelButton.onClick.AddListener(OpenLevelMenu);
        }
    }

    private void UpdateUI()
    {
        if (LevelManager.Instance == null) return;

        int highestUnlocked = LevelManager.Instance.GetHighestUnlockedLevel();
        float completionPercentage = LevelManager.Instance.GetCompletionPercentage();

        // Update start button text
        if (startButtonText != null)
        {
            if (highestUnlocked == 1)
            {
                startButtonText.text = "Start Game";
            }
            else
            {
                // Check if the highest unlocked level is completed
                bool isCurrentCompleted = LevelManager.Instance.IsLevelCompleted(highestUnlocked);
                
                if (isCurrentCompleted && highestUnlocked < LevelManager.Instance.totalLevels)
                {
                    // Player completed current highest, so they'll start the next level
                    startButtonText.text = $"Continue Level {highestUnlocked + 1}";
                }
                else
                {
                    // Player hasn't completed the current highest level yet
                    startButtonText.text = $"Continue Level {highestUnlocked}";
                }
            }
        }

        // Update progress text
        if (progressText != null)
        {
            if (completionPercentage > 0)
            {
                progressText.text = $"Progress: {completionPercentage:F0}% Complete";
                progressText.gameObject.SetActive(true);
            }
            else
            {
                progressText.gameObject.SetActive(false);
            }
        }

        Debug.Log($"[Menu] UI Updated - Highest Unlocked: {highestUnlocked}, Completion: {completionPercentage:F0}%");
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Debug.Log("[Menu] Added EventSystem + InputSystemUIInputModule (New Input System).");
#else
        es.AddComponent<StandaloneInputModule>();
        Debug.Log("[Menu] Added EventSystem + StandaloneInputModule (Old Input Manager).");
#endif
    }

    private void ValidateScene()
    {
        if (!string.IsNullOrEmpty(levelMenuScene) && !Application.CanStreamedLevelBeLoaded(levelMenuScene))
        {
            Debug.LogWarning($"[Menu] Scene \"{levelMenuScene}\" not found in Build Settings.");
        }
    }

    public void StartGame()
    {
        PlayButtonSound();

        // Hide banner ad before leaving menu (if enabled)
        if (hideBannerOnMenuExit)
        {
            HideBannerAd();
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogError("[Menu] LevelManager not found! Loading fallback level.");
            LoadFallbackLevel();
            return;
        }

        int levelToLoad = GetNextLevelToPlay();
        
        // Ensure the level is unlocked before loading
        if (!LevelManager.Instance.IsLevelUnlocked(levelToLoad))
        {
            Debug.LogWarning($"[Menu] Level {levelToLoad} is not unlocked! Loading Level 1 instead.");
            levelToLoad = 1;
        }

        Debug.Log($"[Menu] Starting/Continuing Level {levelToLoad}");
        LevelManager.Instance.LoadLevel(levelToLoad);
    }

    /// <summary>
    /// Determines which level the player should play next
    /// </summary>
    /// <returns>Level number to load</returns>
    private int GetNextLevelToPlay()
    {
        int highestUnlocked = LevelManager.Instance.GetHighestUnlockedLevel();
        
        // If the highest unlocked level is completed, load the next level (if it exists)
        if (LevelManager.Instance.IsLevelCompleted(highestUnlocked))
        {
            int nextLevel = highestUnlocked + 1;
            
            // Check if next level exists and is unlocked
            if (nextLevel <= LevelManager.Instance.totalLevels && 
                LevelManager.Instance.IsLevelUnlocked(nextLevel))
            {
                return nextLevel;
            }
        }
        
        // Otherwise, load the highest unlocked level (player hasn't completed it yet)
        return highestUnlocked;
    }

    private void LoadFallbackLevel()
    {
        Debug.Log($"[Menu] Loading fallback scene by build index: {fallbackBuildIndex}");
        SceneManager.LoadScene(fallbackBuildIndex);
    }

    public void OpenLevelMenu()
    {
        PlayButtonSound();

        // Hide banner ad before leaving menu (if enabled)
        if (hideBannerOnMenuExit)
        {
            HideBannerAd();
        }

        if (!string.IsNullOrEmpty(levelMenuScene) && Application.CanStreamedLevelBeLoaded(levelMenuScene))
        {
            Debug.Log($"[Menu] Opening Level Menu scene: {levelMenuScene}");
            SceneManager.LoadScene(levelMenuScene);
        }
        else
        {
            Debug.LogError($"[Menu] Level Menu scene \"{levelMenuScene}\" not found in Build Settings.");
        }
    }

    public void QuitGame()
    {
        PlayButtonSound();
        Debug.Log("[Menu] Quit pressed. (Works only in a build.)");
        Application.Quit();
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    // Called when this object becomes active (useful when returning from other scenes)
    private void OnEnable()
    {
        // Small delay to ensure LevelManager is ready
        Invoke(nameof(UpdateUI), 0.1f);
        
        // Show banner ad when menu becomes active (if enabled)
        if (showBannerOnMenuLoad && bannerAdManager != null)
        {
            ShowBannerAd();
        }
        
        // Resubscribe to events
        SubscribeToBannerEvents();
    }

    private void OnDisable()
    {
        // Hide banner ad when leaving menu (if enabled)
        if (hideBannerOnMenuExit)
        {
            HideBannerAd();
        }
        
        // Unsubscribe from events
        UnsubscribeFromBannerEvents();
    }

    private void OnDestroy()
    {
        // Ensure we clean up event subscriptions
        UnsubscribeFromBannerEvents();
    }

    // Public method to refresh UI (can be called from other scripts)
    public void RefreshUI()
    {
        UpdateUI();
    }

    #region Development Helper Methods

    [ContextMenu("Show Current Progress")]
    private void ShowCurrentProgress()
    {
        if (LevelManager.Instance != null)
        {
            int highest = LevelManager.Instance.GetHighestUnlockedLevel();
            float completion = LevelManager.Instance.GetCompletionPercentage();
            int nextLevel = GetNextLevelToPlay();
            
            Debug.Log($"=== PROGRESS INFO ===");
            Debug.Log($"Highest Unlocked Level: {highest}");
            Debug.Log($"Completion Percentage: {completion:F1}%");
            Debug.Log($"Next Level to Play: {nextLevel}");
            Debug.Log($"====================");
        }
    }

    [ContextMenu("Test Start Game Logic")]
    private void TestStartGameLogic()
    {
        if (LevelManager.Instance != null)
        {
            int levelToLoad = GetNextLevelToPlay();
            Debug.Log($"[TEST] Start Game would load Level {levelToLoad}");
        }
    }

    [ContextMenu("Show Banner Ad")]
    private void TestShowBannerAd()
    {
        ShowBannerAd();
    }

    [ContextMenu("Hide Banner Ad")]
    private void TestHideBannerAd()
    {
        HideBannerAd();
    }

    [ContextMenu("Toggle Banner Ad")]
    private void TestToggleBannerAd()
    {
        ToggleBannerAd();
    }

    #endregion
}