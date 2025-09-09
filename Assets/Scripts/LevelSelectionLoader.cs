using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectionLoader : MonoBehaviour
{
    [Header("Level Button Settings")]
    [SerializeField] private Button[] levelButtons; // Array of all level buttons
    [SerializeField] private GameObject[] lockIcons; // Lock icons for each level
    [SerializeField] private TextMeshProUGUI[] levelTexts; // Level number texts
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Button backButton;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip lockedSound;
    private AudioSource audioSource;

    void Start()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize level buttons
        SetupLevelButtons();
        UpdateLevelButtonStates();
    }

    void SetupLevelButtons()
    {
        // Auto-find level buttons if not assigned
        if (levelButtons == null || levelButtons.Length == 0)
        {
            levelButtons = FindObjectsOfType<Button>();
            System.Array.Sort(levelButtons, (a, b) => a.name.CompareTo(b.name));
        }

        // Setup button listeners
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] != null)
            {
                int levelNumber = i + 1; // Level numbers start from 1
                levelButtons[i].onClick.RemoveAllListeners();
                levelButtons[i].onClick.AddListener(() => OnLevelButtonClicked(levelNumber));
            }
        }
    }

    void BackToHome()
    {
        SceneManager.LoadScene("GameMenu");
    }

    void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToHome);
        }
    }

    void UpdateLevelButtonStates()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1;
            bool isUnlocked = LevelManager.Instance.IsLevelUnlocked(levelNumber);
            bool isCompleted = LevelManager.Instance.IsLevelCompleted(levelNumber);
            
            // Update button interactability
            if (levelButtons[i] != null)
            {
                levelButtons[i].interactable = isUnlocked;
                
                // Change button color based on lock state
                Image buttonImage = levelButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isUnlocked ? unlockedColor : lockedColor;
                }
            }
            
            // Show/hide lock icons
            if (lockIcons != null && i < lockIcons.Length && lockIcons[i] != null)
            {
                lockIcons[i].SetActive(!isUnlocked);
            }
            
            // Update level text
            if (levelTexts != null && i < levelTexts.Length && levelTexts[i] != null)
            {
                if (isCompleted)
                {
                    levelTexts[i].text = levelNumber.ToString() + " ✓";
                    levelTexts[i].color = Color.green;
                }
                else if (isUnlocked)
                {
                    levelTexts[i].text = levelNumber.ToString();
                    levelTexts[i].color = Color.white;
                }
                else
                {
                    levelTexts[i].text = levelNumber.ToString();
                    levelTexts[i].color = lockedColor;
                }
            }
        }
        
        Debug.Log($"Updated level buttons. Highest unlocked: {LevelManager.Instance.GetHighestUnlockedLevel()}");
    }

    void OnLevelButtonClicked(int levelNumber)
    {
        if (LevelManager.Instance.IsLevelUnlocked(levelNumber))
        {
            PlaySound(buttonClickSound);
            LoadLevel(levelNumber);
        }
        else
        {
            PlaySound(lockedSound);
            ShowLockMessage(levelNumber);
        }
    }

    public void LoadLevel(string levelName)
    {
        // Use LevelManager to load level with unlock check
        LevelManager.Instance.LoadLevelByName(levelName);
    }

    public void LoadLevel(int levelNumber)
    {
        if (LevelManager.Instance.IsLevelUnlocked(levelNumber))
        {
            LevelManager.Instance.LoadLevel(levelNumber);
        }
        else
        {
            ShowLockMessage(levelNumber);
        }
    }

    void ShowLockMessage(int levelNumber)
    {
        int requiredLevel = levelNumber - 1;
        Debug.Log($"Level {levelNumber} is locked! Complete Level {requiredLevel} first.");
        
        // You can add a UI popup here to show the lock message to the player
        // For example, show a toast message or popup panel
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Call this method whenever you return to the level selection screen
    // to refresh the button states (useful if returning from a completed level)
    public void RefreshLevelStates()
    {
        UpdateLevelButtonStates();
    }

    // Optional: Auto-refresh when this object becomes active
    void OnEnable()
    {
        if (LevelManager.Instance != null)
        {
            UpdateLevelButtonStates();
        }
    }

    // Development/Testing methods
    [ContextMenu("Unlock All Levels")]
    void UnlockAllLevels()
    {
        for (int i = 1; i <= LevelManager.Instance.totalLevels; i++)
        {
            LevelManager.Instance.UnlockLevel(i);
        }
        UpdateLevelButtonStates();
    }

    [ContextMenu("Reset All Progress")]
    void ResetAllProgress()
    {
        LevelManager.Instance.ResetAllProgress();
        UpdateLevelButtonStates();
    }
}