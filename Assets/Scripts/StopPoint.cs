using System;
using System.Collections.Generic;
using UnityEngine;

public class StopPoint : MonoBehaviour
{
    public List<StopPoint> neighbors;   // Connected stop points
    public SquareController currentSquare; // Square sitting here
    public float trackHeight = 1f; // Height above ground plane
    public bool isElevated = true; // Track is elevated

    private Renderer rend;
    private Color originalColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    public bool IsOccupied()
    {
        // Clean up null/destroyed references
        if (currentSquare != null && (currentSquare.gameObject == null || !currentSquare.gameObject.activeInHierarchy))
        {
            Debug.Log($"Cleaning up destroyed reference in {name}");
            currentSquare = null;
        }
        
        return currentSquare != null;
    }

    public void Highlight(Color color)
    {
        if (rend != null)
            rend.material.color = color;
    }

    public void ResetHighlight()
    {
        if (rend != null)
            rend.material.color = originalColor;
    }

    // REMOVED THE BROKEN METHODS COMPLETELY
    // No more NotImplementedException!
    
    void OnDrawGizmosSelected()
    {
        // Visual debug for neighbors in Scene view
        if (neighbors != null)
        {
            Gizmos.color = Color.cyan;
            foreach (StopPoint neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
                }
            }
        }
    }
}