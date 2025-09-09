using UnityEngine;
using System.Collections;

public class SquareController : MonoBehaviour
{
    [Header("Movement Settings")]
    public StopPoint currentPoint;
    public float moveSpeed = 5f;
    
    [Header("Cube Properties")]
    public CubeColor cubeColor;
    public bool isFalling = false;
    public float fallSpeed = 10f;
    
    [SerializeField] public bool isMoving = false;
    private Rigidbody rb;
    private GameManager gameManager;
    
    [SerializeField] public bool hasEnteredCollector = false; // New flag to prevent double-triggering

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.isKinematic = true;
        
        gameManager = FindFirstObjectByType<GameManager>();
        
        SetVisualColor();
    }
    
    void SetVisualColor()
    {
        Renderer cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            switch (cubeColor)
            {
                case CubeColor.Red: cubeRenderer.material.color = Color.red; break;
                case CubeColor.Blue: cubeRenderer.material.color = Color.blue; break;
                case CubeColor.Green: cubeRenderer.material.color = Color.green; break;
                case CubeColor.Yellow: cubeRenderer.material.color = Color.yellow; break;
                case CubeColor.Brown: cubeRenderer.material.color = Color.brown; break;
                case CubeColor.Orange: cubeRenderer.material.color = Color.orange; break;
            }
        }
    }

    public void MoveTo(StopPoint targetPoint)
    {
        if (isMoving || targetPoint.IsOccupied() || isFalling) return;

        if (currentPoint != null)
            currentPoint.currentSquare = null;

        StartCoroutine(MoveRoutine(targetPoint));
    }

    public void TravelToBucketAndFall(CollectorBox bucket, System.Collections.Generic.List<StopPoint> pathToNearestStop)
    {
        if (isFalling || isMoving) return;
        
        Debug.Log($"{cubeColor} cube traveling to {bucket.acceptedColor} bucket!");
        
        StartCoroutine(TravelToBucketRoutine(bucket, pathToNearestStop));
    }
    
    IEnumerator TravelToBucketRoutine(CollectorBox bucket, System.Collections.Generic.List<StopPoint> pathToNearestStop)
    {
        isFalling = true;
        hasEnteredCollector = false; // Reset the flag for a new fall
        
        if (pathToNearestStop != null && pathToNearestStop.Count > 0)
        {
            foreach (StopPoint waypoint in pathToNearestStop)
            {
                yield return StartCoroutine(MoveToWaypoint(waypoint));
            }
        }
        
        Vector3 bucketPosition = bucket.transform.position;
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(bucketPosition.x, bucketPosition.y + 0.5f, bucketPosition.z);
        
        if (gameManager != null)
            gameManager.PlayCubeDragAudio();
        
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * fallSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        rb.isKinematic = false;
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        
        Debug.Log($"{cubeColor} cube dropped into collector!");
    }
    
    IEnumerator MoveToWaypoint(StopPoint waypoint)
    {
        if (currentPoint != null)
            currentPoint.currentSquare = null;
            
        Vector3 start = transform.position;
        Vector3 end = waypoint.transform.position;
        float t = 0f;

        if (gameManager != null)
            gameManager.PlayCubeDragAudio();

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;

        currentPoint = waypoint;
        currentPoint.currentSquare = this;

        if (gameManager != null)
            gameManager.PlayStopPointAudio();
        
        yield return new WaitForSeconds(0.1f);
        
        waypoint.currentSquare = null;
    }
    
    public void FallWithPhysics(Vector3 bucketPosition)
    {
        if (isFalling) return;
        
        Debug.Log($"{cubeColor} cube falling with physics!");
        
        isFalling = true;
        hasEnteredCollector = false;
        
        if (gameManager != null)
            gameManager.PlayCubeDragAudio();
        
        if (currentPoint != null)
        {
            currentPoint.currentSquare = null;
            currentPoint = null;
        }
        
        rb.isKinematic = false;
        
        Vector3 direction = (bucketPosition - transform.position).normalized;
        rb.AddForce(direction * 3f + Vector3.down * 2f, ForceMode.Impulse);
        
        StartCoroutine(DestroyAfterTime(3f));
    }
    
    IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }

    IEnumerator MoveRoutine(StopPoint targetPoint)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = targetPoint.transform.position;
        float t = 0f;

        if (gameManager != null)
            gameManager.PlayCubeDragAudio();

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;

        currentPoint = targetPoint;
        currentPoint.currentSquare = this;

        if (gameManager != null)
            gameManager.PlayStopPointAudio();

        isMoving = false;
    }
}