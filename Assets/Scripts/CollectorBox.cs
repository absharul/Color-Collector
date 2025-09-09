using UnityEngine;
using System.Collections;

public class CollectorBox : MonoBehaviour
{
    [Header("Collector Settings")]
    public CubeColor acceptedColor;
    public bool changesColorAfterCollection = true;
    
    private GameManager gameManager;
    private CubeColor[] colorCycle = { CubeColor.Red, CubeColor.Blue, CubeColor.Green, CubeColor.Yellow, CubeColor.Brown, CubeColor.Orange};
    private int currentColorIndex = 0;
    private CubeColor lastCollectedColor;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        
        for (int i = 0; i < colorCycle.Length; i++)
        {
            if (colorCycle[i] == acceptedColor)
            {
                currentColorIndex = i;
                break;
            }
        }
        
        UpdateBucketVisual();
    }
    
    void UpdateBucketVisual()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = GetColorFromEnum(acceptedColor);
        }
    }
    
    public void ChangeToNextColor()
    {
        if (!changesColorAfterCollection) return;
        
        int originalColorIndex = currentColorIndex;
        
        do
        {
            currentColorIndex = (currentColorIndex + 1) % colorCycle.Length;
            acceptedColor = colorCycle[currentColorIndex];
        } while (acceptedColor == lastCollectedColor && currentColorIndex != originalColorIndex);

        UpdateBucketVisual();
        
        Debug.Log($"Bucket changed to {acceptedColor}");
    }
    
    Color GetColorFromEnum(CubeColor color)
    {
        switch(color)
        {
            case CubeColor.Red: return Color.red;
            case CubeColor.Blue: return Color.blue;
            case CubeColor.Green: return Color.green;
            case CubeColor.Yellow: return Color.yellow;
            case CubeColor.Orange: return Color.orange;
            case CubeColor.Brown: return Color.brown;
            default: return Color.white;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        SquareController cube = other.GetComponent<SquareController>();
        if (cube != null)
        {
            if (cube.hasEnteredCollector)
            {
                return;
            }

            cube.hasEnteredCollector = true;

            if (cube != null)
            {
                Debug.Log($"{cube.cubeColor} cube entered {acceptedColor} collector!");

                bool isCorrectColor = cube.cubeColor == acceptedColor;

                if (isCorrectColor)
                {
                    Debug.Log("Correct color collected!");
                    PlayCollectionEffect();

                    if (gameManager != null)
                    {
                        gameManager.CubeCollected(cube, this);
                    }

                    lastCollectedColor = acceptedColor;

                    StartCoroutine(ChangeColorAfterDelay(0.3f));
                }
                else
                {
                    Debug.Log($"Wrong color! {cube.cubeColor} collected by {acceptedColor} bucket - GAME OVER!");
                    if (gameManager != null)
                    {
                        gameManager.WrongCubeCollected(cube, this);
                    }
                }

                StartCoroutine(DeactivateCubeAfterDelay(cube, 0.5f));
            }
        }
    }
    
    
    IEnumerator ChangeColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeToNextColor();
    }
    
    IEnumerator DeactivateCubeAfterDelay(SquareController cube, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cube != null && cube.gameObject != null)
        {
            cube.gameObject.SetActive(false);
        }
    }
    
    void PlayCollectionEffect()
    {
        // Placeholder for effects
    }
}