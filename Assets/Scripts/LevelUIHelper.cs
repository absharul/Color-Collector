using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to automatically setup level selection UI
/// Attach this to your level selection canvas/panel
/// </summary>
public class LevelUIHelper : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private GameObject levelButtonPrefab; // Prefab for level buttons
    [SerializeField] private Transform buttonParent; // Parent object for level buttons (like a grid layout)

    [Header("Generation Settings")]
    [SerializeField] private bool autoGenerateButtons = true;
    [SerializeField] private int totalLevels = 10;

    [Header("Button Styling")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Sprite lockIcon;

    void Start()
    {
        if (autoGenerateButtons)
        {
            GenerateLevelButtons();
        }
    }

    void GenerateLevelButtons()
    {
        if (buttonParent == null)
        {
            Debug.LogError("Button parent is not assigned!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in buttonParent)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Generate level buttons
        for (int i = 1; i <= totalLevels; i++)
        {
            CreateLevelButton(i);
        }
    }

    void CreateLevelButton(int levelNumber)
    {
        GameObject buttonObj;

        // Create button from prefab or default button
        if (levelButtonPrefab != null)
        {
            buttonObj = Instantiate(levelButtonPrefab, buttonParent);
        }
        else
        {
            // Create default button
            buttonObj = new GameObject($"Level{levelNumber}Button");
            buttonObj.transform.SetParent(buttonParent);

            // Add button component
            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = unlockedColor;

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = levelNumber.ToString();
            text.fontSize = 24;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;

            // Set text rect transform
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Add lock icon
            GameObject lockObj = new GameObject("LockIcon");
            lockObj.transform.SetParent(buttonObj.transform);
            Image lockImage = lockObj.AddComponent<Image>();
            if (lockIcon != null)
                lockImage.sprite = lockIcon;
            lockImage.color = Color.white;

            // Set lock icon rect transform
            RectTransform lockRect = lockImage.GetComponent<RectTransform>();
            lockRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(40, 40);
            lockRect.anchoredPosition = Vector2.zero;
        }

        buttonObj.name = $"Level{levelNumber}Button";

        // Setup button functionality
        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => LoadLevel(levelNumber));
        }

        // Update button state
        UpdateButtonState(buttonObj, levelNumber);
    }

    void LoadLevel(int levelNumber)
    {
        if (LevelManager.Instance.IsLevelUnlocked(levelNumber))
        {
            LevelManager.Instance.LoadLevel(levelNumber);
        }
        else
        {
            Debug.Log($"Level {levelNumber} is locked!");
            // Show lock message or play lock sound
        }
    }

    void UpdateButtonState(GameObject buttonObj, int levelNumber)
    {
        bool isUnlocked = LevelManager.Instance.IsLevelUnlocked(levelNumber);
        bool isCompleted = LevelManager.Instance.IsLevelCompleted(levelNumber);

        Button button = buttonObj.GetComponent<Button>();
        Image buttonImage = buttonObj.GetComponent<Image>();

        // Update button interactability and color
        if (button != null)
        {
            button.interactable = isUnlocked;
        }

        if (buttonImage != null)
        {
            if (isCompleted)
                buttonImage.color = completedColor;
            else if (isUnlocked)
                buttonImage.color = unlockedColor;
            else
                buttonImage.color = lockedColor;
        }

        // Update text
        TextMeshProUGUI text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            if (isCompleted)
            {
                text.text = levelNumber.ToString() + " ✓";
                text.color = Color.white;
            }
            else
            {
                text.text = levelNumber.ToString();
                text.color = isUnlocked ? Color.black : Color.gray;
            }
        }

        // Update lock icon
        Transform lockIcon = buttonObj.transform.Find("LockIcon");
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(!isUnlocked);
        }
    }

    // Call this to refresh all button states
    public void RefreshButtonStates()
    {
        for (int i = 0; i < buttonParent.childCount; i++)
        {
            Transform child = buttonParent.GetChild(i);
            string buttonName = child.name;

            // Extract level number from button name
            if (buttonName.StartsWith("Level") && buttonName.EndsWith("Button"))
            {
                string numberPart = buttonName.Substring(5, buttonName.Length - 11); // Remove "Level" and "Button"
                if (int.TryParse(numberPart, out int levelNumber))
                {
                    UpdateButtonState(child.gameObject, levelNumber);
                }
            }
        }
    }

    void OnEnable()
    {
        // Refresh button states when UI becomes active
        if (Application.isPlaying && buttonParent != null && buttonParent.childCount > 0)
        {
            Invoke(nameof(RefreshButtonStates), 0.1f); // Small delay to ensure LevelManager is ready
        }
    }

    // Editor helper methods
    [ContextMenu("Generate Buttons")]
    void EditorGenerateButtons()
    {
        if (LevelManager.Instance == null)
        {
            // Create temporary level manager for editor
            GameObject tempManager = new GameObject("TempLevelManager");
            tempManager.AddComponent<LevelManager>();
        }

        GenerateLevelButtons();
    }

    [ContextMenu("Clear Buttons")]
    void ClearButtons()
    {
        if (buttonParent == null) return;

        for (int i = buttonParent.childCount - 1; i >= 0; i--)
        {
            Transform child = buttonParent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}