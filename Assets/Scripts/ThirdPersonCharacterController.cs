using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ThirdPersonCharacterController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = 9.81f;
    public float jumpHeight = 2.4f;

    private CharacterController controller;
    private Animator animator;
    private Transform cameraTransform;
    private float verticalVelocity = 0.0f;
    private float groundedTimer = 0.0f;

    private bool isDriving = false;
    private Transform currentVehicle = null;
    private CharacterController vehicleController = null;
    private float vehicleVerticalVelocity = 0.0f;
    private float currentVehicleSpeed = 0.0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Force player starting position to the new adjusted coordinates
        controller.enabled = false;
        transform.position = new Vector3(150.00f, 0.30f, 87.04f);
        controller.enabled = true;

        transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

        gameObject.AddComponent<NavigationArrow>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        bool isFPressed = keyboard != null && keyboard.fKey.wasPressedThisFrame;

        if (isDriving)
        {
            if (isFPressed)
            {
                ExitVehicle();
                return;
            }

            DriveVehicle();
            return;
        }

        if (isFPressed)
        {
            TryBoardVehicle();
        }

        // Read Input using the new Input System directly
        float horizontal = 0f;
        float vertical = 0f;

        bool isJumpPressed = false;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.spaceKey.wasPressedThisFrame) isJumpPressed = true;
        }

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 moveDirection = Vector3.zero;

        // Calculate move direction relative to camera
        if (inputDir.magnitude > 0.1f)
        {
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;

            if (cameraTransform != null)
            {
                camForward = cameraTransform.forward;
                camRight = cameraTransform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();
            }

            moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;

            // Rotate player towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Set Animator running speed
            animator.SetFloat("Speed", 1.0f);
        }
        else
        {
            // Set Animator idle speed
            animator.SetFloat("Speed", 0.0f);
        }

        // Check grounding using robust spherecast
        bool isGrounded = CheckGrounded();
        animator.SetBool("Grounded", isGrounded);

        // Apply Gravity & Grounding
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2.0f; // slightly stronger force to keep grounded on bumps
            groundedTimer = 0.15f; // coyote time window (0.15 seconds)
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
            groundedTimer -= Time.deltaTime;
        }

        // Apply Jump
        if (groundedTimer > 0f && isJumpPressed)
        {
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            animator.SetTrigger("Jump");
            groundedTimer = 0f; // consume jump to prevent double jumping
        }

        // Combine horizontal movement and vertical movement correctly
        Vector3 move = moveDirection * moveSpeed;
        move.y = verticalVelocity;

        // Move the controller
        controller.Move(move * Time.deltaTime);
    }

    private bool CheckGrounded()
    {
        if (controller.isGrounded) return true;

        float radius = controller.radius * 0.9f;
        Vector3 origin = transform.position + controller.center + Vector3.down * (controller.height * 0.5f - radius);
        float castDistance = 0.15f;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, Vector3.down, castDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider == controller || hit.collider.transform.IsChildOf(transform))
                continue;

            if (Vector3.Angle(hit.normal, Vector3.up) < controller.slopeLimit)
            {
                return true;
            }
        }
        return false;
    }

    private void TryBoardVehicle()
    {
        GameObject vehicleGo = null;
        float nearestDist = float.MaxValue;

        // Find colliders within 1.0 meter from player center
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1.0f);
        foreach (var col in colliders)
        {
            Transform current = col.transform;
            while (current != null)
            {
                string name = current.name;
                if (name.Contains("Vehicle_Taxi") || name.Contains("Vehicle_Police"))
                {
                    float dist = Vector3.Distance(transform.position, current.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        vehicleGo = current.gameObject;
                    }
                    break;
                }
                current = current.parent;
            }
        }

        if (vehicleGo != null)
        {
            isDriving = true;
            currentVehicle = vehicleGo.transform;

            // Disable player controller and physics
            controller.enabled = false;

            // Hide player meshes and rig
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            // Parent player to vehicle
            transform.SetParent(currentVehicle);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // Disable all existing colliders on the vehicle to prevent self-collision
            Collider[] vehicleColliders = currentVehicle.GetComponentsInChildren<Collider>();
            foreach (var col in vehicleColliders)
            {
                if (!(col is CharacterController))
                {
                    col.enabled = false;
                }
            }

            // Setup vehicle controller
            vehicleController = currentVehicle.GetComponent<CharacterController>();
            if (vehicleController == null)
            {
                vehicleController = currentVehicle.gameObject.AddComponent<CharacterController>();
            }
            // Adjust radius and height to fit the car box so the bottom is exactly at ground level (Y = 0)
            vehicleController.radius = 0.7f;
            vehicleController.height = 1.4f;
            vehicleController.center = new Vector3(0f, 0.7f, 0f);
            vehicleVerticalVelocity = 0.0f;
            currentVehicleSpeed = 0.0f;

            // Make camera 1.5x further when driving
            if (cameraTransform != null)
            {
                ThirdPersonCameraController camController = cameraTransform.GetComponent<ThirdPersonCameraController>();
                if (camController != null)
                {
                    camController.offset = new Vector3(0f, 2.6f * 1.5f, -6.5f * 1.5f);
                }
            }

            // Mission 1 check: Boarded Taxi
            if (currentVehicle.name.Contains("Vehicle_Taxi"))
            {
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.SetState(1);
                }
            }
        }
    }

    private void DriveVehicle()
    {
        if (currentVehicle == null || vehicleController == null) return;

        // Read Input
        float horizontal = 0f;
        float vertical = 0f;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        }

        // Apply Movement with 3-second acceleration and curve slowdown factor
        float carSpeed = moveSpeed * 2.5f;
        float curveFactor = (Mathf.Abs(horizontal) > 0.1f) ? 0.7f : 1.0f; // 30% slower during curves
        float targetSpeed = vertical * carSpeed * curveFactor;
        
        if (vertical == 0f)
        {
            // Decelerate quickly to 0 (over 0.35 seconds) to create a slight residual momentum/inertia
            float decelRate = carSpeed / 0.35f;
            currentVehicleSpeed = Mathf.MoveTowards(currentVehicleSpeed, 0f, decelRate * Time.deltaTime);
        }
        else
        {
            // Acceleration rate: reaches top speed in exactly 3.0 seconds
            float accelRate = carSpeed / 3.0f;
            currentVehicleSpeed = Mathf.MoveTowards(currentVehicleSpeed, targetSpeed, accelRate * Time.deltaTime);
        }

        Vector3 move = currentVehicle.forward * currentVehicleSpeed;

        // Apply Rotation (Steering) - only when vehicle is moving
        if (Mathf.Abs(currentVehicleSpeed) > 0.05f)
        {
            float speedRatio = Mathf.Abs(currentVehicleSpeed) / carSpeed;
            // Scale turning speed based on velocity (20% at low speed up to 100% at max speed) to prevent top-spinning
            float turnSpeedFactor = Mathf.Lerp(0.2f, 1.0f, speedRatio);
            // Reduce overall turn speed by 30% as requested
            float turnSpeed = rotationSpeed * 10f * turnSpeedFactor * 0.7f;

            float direction = currentVehicleSpeed >= 0f ? 1f : -1f;
            currentVehicle.Rotate(0f, horizontal * turnSpeed * direction * Time.deltaTime, 0f);
        }

        // Apply Gravity
        if (vehicleController.isGrounded)
        {
            vehicleVerticalVelocity = -2.0f;
        }
        else
        {
            vehicleVerticalVelocity -= gravity * Time.deltaTime;
        }
        move.y = vehicleVerticalVelocity;

        // Move vehicle
        vehicleController.Move(move * Time.deltaTime);

        // Keep player centered in the vehicle
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void ExitVehicle()
    {
        if (currentVehicle == null) return;

        // Unparent player
        transform.SetParent(null);

        // Position player to the right side of the vehicle
        transform.position = currentVehicle.position + currentVehicle.right * 1.8f + Vector3.up * 0.5f;
        transform.rotation = Quaternion.Euler(0f, currentVehicle.eulerAngles.y, 0f);

        // Show player meshes and rig
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }

        // Re-enable player controller
        controller.enabled = true;

        // Re-enable all existing colliders on the vehicle
        Collider[] vehicleColliders = currentVehicle.GetComponentsInChildren<Collider>();
        foreach (var col in vehicleColliders)
        {
            if (!(col is CharacterController))
            {
                col.enabled = true;
            }
        }

        // Clean up vehicle controller
        if (vehicleController != null)
        {
            Destroy(vehicleController);
            vehicleController = null;
        }

        // Restore camera distance
        if (cameraTransform != null)
        {
            ThirdPersonCameraController camController = cameraTransform.GetComponent<ThirdPersonCameraController>();
            if (camController != null)
            {
                camController.offset = new Vector3(0f, 2.6f, -6.5f);
            }
        }

        // Mission 4 check: Exited Taxi (Only if Mission 3 is already cleared)
        if (currentVehicle != null && currentVehicle.name.Contains("Vehicle_Taxi"))
        {
            if (MissionManager.Instance != null && MissionManager.Instance.currentState == 3)
            {
                MissionManager.Instance.SetState(4);
            }
        }

        isDriving = false;
        currentVehicle = null;
    }
}
