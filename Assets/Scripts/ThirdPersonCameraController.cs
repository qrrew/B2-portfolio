using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2.6f, -6.5f);
    public float yawSpeed = 150.0f;
    public float pitchSpeed = 100.0f;
    public float minPitch = -10.0f;
    public float maxPitch = 70.0f;

    private float currentYaw = 0.0f;
    private float currentPitch = 20.0f;

    private bool isIntroTransition = true;
    private float introElapsedTime = 0.0f;
    private Vector3 introStartPos = new Vector3(-9.712f, 155.920f, 137.189f);
    private Quaternion introStartRot = Quaternion.Euler(60.299f, 115.129f, 0.259f);

    void Start()
    {
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
        }
        
        // Immediately set camera to intro starting position/rotation
        transform.position = introStartPos;
        transform.rotation = introStartRot;

        // Lock cursor so it doesn't move outside game window while playing
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Toggle cursor lock with Escape key using the new Input System
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Rotate camera with mouse input using the new Input System (only when not in transition)
        if (!isIntroTransition && Cursor.lockState == CursorLockMode.Locked)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                // Multiply mouse delta by a scaling factor to match old Input.GetAxis behavior
                float mouseX = mouse.delta.x.ReadValue() * 0.05f;
                float mouseY = mouse.delta.y.ReadValue() * 0.05f;

                currentYaw += mouseX * yawSpeed * Time.deltaTime;
                currentPitch -= mouseY * pitchSpeed * Time.deltaTime;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }
        }

        if (isIntroTransition)
        {
            introElapsedTime += Time.deltaTime;
            if (introElapsedTime <= 2.0f)
            {
                transform.position = introStartPos;
                transform.rotation = introStartRot;
                
                // Keep initial camera variables locked to match player rotation
                currentYaw = target.eulerAngles.y;
                currentPitch = 20.0f;
                return;
            }
            else if (introElapsedTime <= 6.0f)
            {
                float t = (introElapsedTime - 2.0f) / 4.0f;
                t = Mathf.SmoothStep(0f, 1f, t);

                // Calculate normal position and target look rotation
                Quaternion normalRot = Quaternion.Euler(currentPitch, currentYaw, 0f);
                Vector3 normalPos = target.position + normalRot * offset;

                transform.position = Vector3.Lerp(introStartPos, normalPos, t);

                Vector3 targetLookDir = (target.position + Vector3.up * 1.0f) - transform.position;
                if (targetLookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetLook = Quaternion.LookRotation(targetLookDir);
                    transform.rotation = Quaternion.Slerp(introStartRot, targetLook, t);
                }
                return;
            }
            else
            {
                isIntroTransition = false;
            }
        }

        // Calculate rotation and target position
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 position = target.position + rotation * offset;

        // Set camera position and look at target
        transform.position = position;
        transform.LookAt(target.position + Vector3.up * 1.0f);
    }
}
