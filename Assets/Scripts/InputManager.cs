using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    private SquareController selectedSquare;
    private bool isDragging = false;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Vector2 initialPointerPosition;

    [SerializeField] public AudioClip pathBlockedAudio;
    private AudioSource pathBlockedAudioSource;

    public Image incorrectPathImage;
    public float displayDuration = 2f;
    
    [Header("Drag Settings")]
    public float holdThreshold = 0.3f; // Time to hold before drag starts
    public float dragThreshold = 10f; // Minimum pixel distance to start dragging

    void Start()
    {
        // Ensure there's an AudioSource on this GameObject
        pathBlockedAudioSource = gameObject.AddComponent<AudioSource>();
        pathBlockedAudioSource.playOnAwake = false;
    }

    void Update()
    {
        HandleDragInput();
    }

    void HandleDragInput()
    {
        if (Pointer.current == null) return;

        Vector2 currentPointerPosition = Pointer.current.position.ReadValue();

        // Handle pointer press start
        if (Pointer.current.press.wasPressedThisFrame)
        {
            StartHold(currentPointerPosition);
        }
        // Handle pointer hold
        else if (Pointer.current.press.isPressed)
        {
            UpdateHoldAndDrag(currentPointerPosition);
        }
        // Handle pointer release
        else if (Pointer.current.press.wasReleasedThisFrame)
        {
            EndHoldOrDrag(currentPointerPosition);
        }
    }

    void StartHold(Vector2 pointerPosition)
    {
        initialPointerPosition = pointerPosition;
        holdTimer = 0f;
        isHolding = false;
        isDragging = false;
        selectedSquare = null;

        // Check what we're clicking on
        Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Square"))
            {
                selectedSquare = hit.collider.GetComponent<SquareController>();
                isHolding = true;
            }
            else if (hit.collider.CompareTag("CollectorBox"))
            {
                // Handle immediate collector box tap
                CollectorBox bucket = hit.collider.GetComponent<CollectorBox>();
                DropCubesIntoBucket(bucket);
            }
        }
    }

    void UpdateHoldAndDrag(Vector2 currentPointerPosition)
    {
        if (!isHolding && !isDragging) return;

        // Update hold timer
        if (isHolding)
        {
            holdTimer += Time.deltaTime;

            // Check if we should start dragging
            float dragDistance = Vector2.Distance(initialPointerPosition, currentPointerPosition);
            
            if (holdTimer >= holdThreshold || dragDistance >= dragThreshold)
            {
                StartDrag();
            }
        }

        // Update drag visuals
        if (isDragging && selectedSquare != null)
        {
            UpdateDragHighlights();
        }
    }

    void StartDrag()
    {
        if (selectedSquare == null) return;

        isHolding = false;
        isDragging = true;
        
        // Highlight all valid moves for the selected square
        HighlightValidMoves(selectedSquare);
        
        // Optional: Add visual feedback that we're dragging
        Debug.Log($"Started dragging {selectedSquare.name}");
    }

    void UpdateDragHighlights()
    {
        if (Pointer.current == null || selectedSquare == null) return;

        Vector2 pointerPosition = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(pointerPosition);

        ResetHighlights();

        HighlightValidMoves(selectedSquare);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("StopPoint"))
            {
                StopPoint targetPoint = hit.collider.GetComponent<StopPoint>();
                bool isNeighbor = selectedSquare.currentPoint.neighbors.Contains(targetPoint);
                bool isOccupied = targetPoint.IsOccupied();

                if (isNeighbor && !isOccupied)
                {
                    targetPoint.Highlight(Color.yellow);
                }
                else
                {
                    targetPoint.Highlight(Color.red);
                }
            }
        }
    }

    void EndHoldOrDrag(Vector2 pointerPosition)
    {
        if (isDragging && selectedSquare != null)
        {
            // Handle drop
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"Drop hit object: {hit.collider.name} with tag: {hit.collider.tag}");

                if (hit.collider.CompareTag("StopPoint"))
                {
                    StopPoint targetPoint = hit.collider.GetComponent<StopPoint>();
                    
                    bool isNeighbor = selectedSquare.currentPoint.neighbors.Contains(targetPoint);
                    bool isOccupied = targetPoint.IsOccupied();

                    if (isNeighbor && !isOccupied)
                    {
                        selectedSquare.MoveTo(targetPoint);
                        Debug.Log($"Successfully moved {selectedSquare.name} to {targetPoint.name}");
                    }
                    else
                    {
                        PlayPathBlockedSound();
                        Debug.Log($"Cannot move to {targetPoint.name} - neighbor: {isNeighbor}, occupied: {isOccupied}");
                    }
                }
                else if (hit.collider.CompareTag("CollectorBox"))
                {
                    // Handle drop onto collector box
                    CollectorBox bucket = hit.collider.GetComponent<CollectorBox>();
                    DropSpecificCubeIntoBucket(selectedSquare, bucket);
                }
                else
                {
                    Debug.Log("Dropped on invalid target");
                    PlayPathBlockedSound();
                }
            }
            else
            {
                Debug.Log("Dropped on empty space");
                PlayPathBlockedSound();
            }
        }
        else if (isHolding && selectedSquare != null)
        {
            // Short tap without drag - could be used for selection or other actions
            Debug.Log($"Short tap on {selectedSquare.name}");
        }

        // Reset all states
        ResetHighlights();
        selectedSquare = null;
        isDragging = false;
        isHolding = false;
        holdTimer = 0f;
    }

    private IEnumerator ShowImageTemporarily()
    {
        incorrectPathImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        incorrectPathImage.gameObject.SetActive(false);
    }

    void HighlightValidMoves(SquareController square)
    {
        if (square.currentPoint == null) return;

        foreach (StopPoint neighbor in square.currentPoint.neighbors)
        {
            if (!neighbor.IsOccupied())
                neighbor.Highlight(Color.teal);
        }
    }

    void ResetHighlights()
    {
        StopPoint[] allStops = FindObjectsByType<StopPoint>(FindObjectsSortMode.None);
        foreach (StopPoint stop in allStops)
            stop.ResetHighlight();
    }

    // Original method for collector box functionality
    void DropCubesIntoBucket(CollectorBox bucket)
    {
        SquareController[] allCubes = FindObjectsByType<SquareController>(FindObjectsSortMode.None);
        bool foundMatchingCube = false;

        foreach (SquareController cube in allCubes)
        {
            if (cube.cubeColor == bucket.acceptedColor && !cube.isFalling && !cube.isMoving)
            {
                foundMatchingCube = true;

                StopPoint nearestStopToBucket = FindNearestStopPointToBucket(bucket);
                if (nearestStopToBucket != null)
                {
                    var pathToNearestStop = FindPath(cube.currentPoint, nearestStopToBucket);

                    if (pathToNearestStop != null && IsPathClear(pathToNearestStop))
                        cube.TravelToBucketAndFall(bucket, pathToNearestStop);
                    else
                        PlayPathBlockedSound();
                }
            }
        }

        if (!foundMatchingCube)
            Debug.Log($"No matching {bucket.acceptedColor} cubes found to drop!");

        ResetHighlights();
        selectedSquare = null;
    }

    // New method for dropping a specific cube into bucket
    void DropSpecificCubeIntoBucket(SquareController cube, CollectorBox bucket)
    {
        if (cube.cubeColor == bucket.acceptedColor && !cube.isFalling && !cube.isMoving)
        {
            StopPoint nearestStopToBucket = FindNearestStopPointToBucket(bucket);
            if (nearestStopToBucket != null)
            {
                var pathToNearestStop = FindPath(cube.currentPoint, nearestStopToBucket);

                if (pathToNearestStop != null && IsPathClear(pathToNearestStop))
                {
                    cube.TravelToBucketAndFall(bucket, pathToNearestStop);
                    Debug.Log($"Successfully dropped {cube.name} into bucket");
                }
                else
                {
                    PlayPathBlockedSound();
                    Debug.Log("Path to bucket is blocked");
                }
            }
        }
        else
        {
            PlayPathBlockedSound();
            Debug.Log($"Cannot drop {cube.name} into bucket - wrong color or cube is busy");
        }
    }

    StopPoint FindNearestStopPointToBucket(CollectorBox bucket)
    {
        StopPoint nearest = null;
        float shortestDistance = Mathf.Infinity;

        StopPoint[] allStops = FindObjectsByType<StopPoint>(FindObjectsSortMode.None);
        foreach (StopPoint stop in allStops)
        {
            float distance = Vector3.Distance(stop.transform.position, bucket.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = stop;
            }
        }

        return nearest;
    }

    System.Collections.Generic.List<StopPoint> FindPath(StopPoint start, StopPoint end)
    {
        if (start == end) return new System.Collections.Generic.List<StopPoint>();

        var queue = new System.Collections.Generic.Queue<StopPoint>();
        var cameFrom = new System.Collections.Generic.Dictionary<StopPoint, StopPoint>();
        var visited = new System.Collections.Generic.HashSet<StopPoint>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            StopPoint current = queue.Dequeue();

            if (current == end)
            {
                var path = new System.Collections.Generic.List<StopPoint>();
                StopPoint step = end;

                while (step != start)
                {
                    path.Add(step);
                    step = cameFrom[step];
                }

                path.Reverse();
                return path;
            }

            foreach (StopPoint neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return null;
    }

    bool IsPathClear(System.Collections.Generic.List<StopPoint> path)
    {
        foreach (StopPoint stop in path)
        {
            if (stop.IsOccupied())
            {
                PlayPathBlockedSound();
                return false;
            }
        }
        return true;
    }

    void PlayPathBlockedSound()
    {
        if (pathBlockedAudio != null && pathBlockedAudioSource != null)
            pathBlockedAudioSource.PlayOneShot(pathBlockedAudio);
        if (incorrectPathImage != null)
        {
            StartCoroutine(ShowImageTemporarily());
        }
    }
}