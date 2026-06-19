using UnityEngine;
using System.Linq;

public class TaxiPassenger : MonoBehaviour
{
    private Animator animator;
    private bool isWalking = false;
    private Transform taxiTransform = null;
    private float walkSpeed = 1.5f; // 5.0f * 0.3f
    
    public float detectionDistance = 6.0f; // Distance from taxi pivot to start walking
    public float stopDistance = 1.5f;      // Distance from taxi pivot to complete boarding (disappear)

    void Start()
    {
        animator = GetComponent<Animator>();
        ApplyAnimatorOverride();
        transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
    }

    void OnEnable()
    {
        ApplyAnimatorOverride();
    }

    private void ApplyAnimatorOverride()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) return;

        // Force Animator Culling Mode to AlwaysAnimate to prevent culling freeze bugs
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.applyRootMotion = false; // Disable root motion to ensure manual transform movement works

        RuntimeAnimatorController baseController = animator.runtimeAnimatorController;
        if (baseController == null) return;

        // Unwrap existing override controller if present to get the clean base controller
        if (baseController is AnimatorOverrideController existingOverride)
        {
            baseController = existingOverride.runtimeAnimatorController;
        }

        if (baseController != null)
        {
            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            string walkFbxPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx";
            
#if UNITY_EDITOR
            Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(walkFbxPath);
            if (subAssets != null)
            {
                // Filter out non-looping "__preview__" clips to prevent freeze/glide bugs
                AnimationClip walkClip = subAssets.OfType<AnimationClip>()
                    .FirstOrDefault(c => c.name.Contains("Walk01_Forward") && !c.name.StartsWith("__preview__"));
                if (walkClip != null)
                {
                    overrideController["HumanM@Run01_Forward"] = walkClip;
                    animator.runtimeAnimatorController = overrideController;
                }
            }
#endif
        }
    }

    void Update()
    {
        // Only act if we are in Mission 2 (currentState == 1)
        if (MissionManager.Instance == null || MissionManager.Instance.currentState != 1)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 0.0f);
            }
            return;
        }

        // Dynamically locate the active taxi the player is driving
        ThirdPersonCharacterController player = FindAnyObjectByType<ThirdPersonCharacterController>();
        if (player != null && player.transform.parent != null && player.transform.parent.name.Contains("Vehicle_Taxi"))
        {
            taxiTransform = player.transform.parent;
        }

        if (taxiTransform == null)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 0.0f);
            }
            return;
        }

        // Calculate distance to taxi
        float distance = Vector3.Distance(transform.position, taxiTransform.position);

        if (!isWalking)
        {
            // When taxi is within detection distance, start walking
            if (distance <= detectionDistance)
            {
                isWalking = true;
            }
        }

        // Keep updating animator parameters to ensure motion plays
        if (isWalking)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 1.0f); // Triggers Walk (overridden)
                animator.SetBool("Grounded", true);
                animator.ResetTrigger("Jump");
            }

            // If we are close enough to the taxi (stopDistance), complete boarding
            if (distance <= stopDistance)
            {
                MissionManager.Instance.SetState(2);
                Destroy(gameObject);
                return;
            }

            // Rotate towards taxi (Y axis only)
            Vector3 targetDir = taxiTransform.position - transform.position;
            targetDir.y = 0;
            if (targetDir.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), 10f * Time.deltaTime);
            }

            // Move forward
            transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime);

            // Ground Snapping: Force Y coordinate to stick to the road/ground collider
            Vector3 currentPos = transform.position;
            Ray ray = new Ray(new Vector3(currentPos.x, currentPos.y + 2.0f, currentPos.z), Vector3.down);
            RaycastHit hit;
            
            // Temporary ignore own collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (Physics.Raycast(ray, out hit, 10.0f))
            {
                currentPos.y = hit.point.y;
                transform.position = currentPos;
            }

            if (col != null) col.enabled = true;
        }
        else
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 0.0f);
            }
        }
    }
}
