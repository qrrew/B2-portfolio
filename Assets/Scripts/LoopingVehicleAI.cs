using UnityEngine;

public class LoopingVehicleAI : MonoBehaviour
{
    public Vector3[] waypoints;
    public float speed = 4.17f; // Will be set dynamically based on player's taxi speed
    
    private int currentWaypointIndex = 0;
    private Rigidbody rb;
    private float currentSpeed = 0f;
    private float previousSpeed = 0f;
    private float currentPitch = 0f;
    private float pitchVelocity = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Dynamically set speed to 1/3 of player's taxi speed
        ThirdPersonCharacterController player = FindAnyObjectByType<ThirdPersonCharacterController>();
        if (player != null)
        {
            speed = (player.moveSpeed * 2.5f) / 3.0f;
        }
        else
        {
            speed = 4.17f;
        }
        currentSpeed = speed;
        previousSpeed = speed;
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 target = waypoints[currentWaypointIndex];
        
        // Horizontal direction
        Vector3 targetDir = target - transform.position;
        targetDir.y = 0;

        float distance = targetDir.magnitude;

        // Waypoint arrival check (within 1.5 meters is safe and gives smooth turns)
        if (distance < 1.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            target = waypoints[currentWaypointIndex];
            targetDir = target - transform.position;
            targetDir.y = 0;
        }

        // Check for obstacles ahead (vehicles, player, buildings, etc.)
        bool isBlocked = false;
        Vector3 forward = transform.forward;
        Vector3 boxCenter = transform.position + transform.up * 1.0f;
        Vector3 boxHalfExtents = new Vector3(1.4f, 0.5f, 1.0f); // Width = 2.8m, Height = 1.0m, Length = 2.0m
        float stopDistance = 8.5f; // stop if obstacle is within 8.5m from box center (longer distance for early detection)

        RaycastHit[] obstacleHits = Physics.BoxCastAll(
            boxCenter, 
            boxHalfExtents, 
            forward, 
            transform.rotation, 
            stopDistance, 
            Physics.AllLayers, 
            QueryTriggerInteraction.Ignore
        );
        
        foreach (var obsHit in obstacleHits)
        {
            if (obsHit.collider.transform.IsChildOf(transform) || obsHit.collider.transform == transform)
                continue;
            if (obsHit.distance == 0f)
                continue;
                
            isBlocked = true;
            break;
        }

        float targetSpeed = isBlocked ? 0f : speed;

        // Acceleration and Deceleration logic
        float accelRate = speed / 2.0f;  // Reaches full speed in 2.0 seconds
        float decelRate = speed / 0.5f;  // Stops in 0.5 seconds (quick but smooth)

        previousSpeed = currentSpeed;
        if (targetSpeed > currentSpeed)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, decelRate * Time.deltaTime);
        }

        // Move towards target in world space
        Vector3 moveDir = targetDir.normalized;
        transform.position += moveDir * currentSpeed * Time.deltaTime;

        // Pitch spring simulation for visual brake dive (잔반동) - Scaled 1.5x larger
        float accel = Time.deltaTime > 0f ? (currentSpeed - previousSpeed) / Time.deltaTime : 0f;
        float targetPitch = -accel * 0.225f; // 1.5x (0.15f * 1.5)
        
        float springStrength = 15.0f;
        float damping = 4.0f;
        
        pitchVelocity += (targetPitch - currentPitch) * springStrength * Time.deltaTime;
        pitchVelocity -= pitchVelocity * damping * Time.deltaTime;
        currentPitch += pitchVelocity * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, -4.5f, 7.5f); // 1.5x limits

        // Smoothly rotate towards target direction (yaw only) + apply spring pitch
        if (targetDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            Quaternion SlerpedYaw = Quaternion.Slerp(transform.rotation, targetRot, 5.0f * Time.deltaTime);
            Vector3 euler = SlerpedYaw.eulerAngles;
            transform.rotation = Quaternion.Euler(currentPitch, euler.y, 0f);
        }
        else
        {
            Vector3 euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentPitch, euler.y, 0f);
        }

        // Ground Snapping: Keep wheels on the road
        Vector3 currentPos = transform.position;
        Ray ray = new Ray(new Vector3(currentPos.x, currentPos.y + 2.0f, currentPos.z), Vector3.down);
        RaycastHit hit;
        
        // Ignore own colliders
        Collider[] ownColliders = GetComponentsInChildren<Collider>();
        foreach (var col in ownColliders) col.enabled = false;

        if (Physics.Raycast(ray, out hit, 10.0f))
        {
            currentPos.y = hit.point.y;
            transform.position = currentPos;
        }

        foreach (var col in ownColliders) col.enabled = true;
    }
}
