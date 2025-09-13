using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    private SquareController selectedSquare;

    [SerializeField] public AudioClip pathBlockedAudio;
    private AudioSource pathBlockedAudioSource;

    public Image incorrectPathImage;

    public float displayDuration = 2f;

    void Start()
    {
        // Ensure there’s an AudioSource on this GameObject
        pathBlockedAudioSource = gameObject.AddComponent<AudioSource>();
        pathBlockedAudioSource.playOnAwake = false;
    }



    void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"Hit object: {hit.collider.name} with tag: {hit.collider.tag}");

                RaycastHit[] allHits = Physics.RaycastAll(ray);
                Debug.Log($"All objects hit ({allHits.Length}):");
                foreach (RaycastHit h in allHits)
                {
                    Debug.Log($"  - {h.collider.name} (tag: {h.collider.tag})");
                }

                if (hit.collider.CompareTag("Square"))
                {
                    if (selectedSquare != null)
                        ResetHighlights();

                    selectedSquare = hit.collider.GetComponent<SquareController>();
                    HighlightValidMoves(selectedSquare);
                }
                else if (hit.collider.CompareTag("StopPoint"))
                {
                    if (selectedSquare != null)
                    {
                        StopPoint targetPoint = hit.collider.GetComponent<StopPoint>();

                        bool isNeighbor = selectedSquare.currentPoint.neighbors.Contains(targetPoint);
                        bool isOccupied = targetPoint.IsOccupied();

                        if (isNeighbor && !isOccupied)
                            selectedSquare.MoveTo(targetPoint);
                        else
                            PlayPathBlockedSound();

                        ResetHighlights();
                        selectedSquare = null;
                    }
                }
                else if (hit.collider.CompareTag("CollectorBox"))
                {
                    CollectorBox bucket = hit.collider.GetComponent<CollectorBox>();
                    DropCubesIntoBucket(bucket);
                }
                else
                {
                    ResetHighlights();
                    selectedSquare = null;
                }
            }
            else
            {
                ResetHighlights();
                selectedSquare = null;
            }
        }
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

