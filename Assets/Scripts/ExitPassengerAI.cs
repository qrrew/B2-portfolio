using UnityEngine;
using System.Linq;

public class ExitPassengerAI : MonoBehaviour
{
    private Animator animator;
    private float walkSpeed = 1.5f; // Same 0.3x speed
    private float startZ;
    private float targetDistance = 8.0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        ApplyAnimatorOverride();
        startZ = transform.position.z;
        
        // Force look towards Positive Z direction
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
    }

    void OnEnable()
    {
        ApplyAnimatorOverride();
    }

    private void ApplyAnimatorOverride()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) return;

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
        if (animator != null)
        {
            animator.SetFloat("Speed", 1.0f); // Keep Walk animation playing
            animator.SetBool("Grounded", true);
            animator.ResetTrigger("Jump");
        }

        // Keep looking forward
        transform.rotation = Quaternion.LookRotation(Vector3.forward);

        // Move forward in Z positive direction
        transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime);

        // Ground Snapping: Force Y coordinate to stick to the road/ground collider
        Vector3 currentPos = transform.position;
        Ray ray = new Ray(new Vector3(currentPos.x, currentPos.y + 2.0f, currentPos.z), Vector3.down);
        RaycastHit hit;
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (Physics.Raycast(ray, out hit, 10.0f))
        {
            currentPos.y = hit.point.y;
            transform.position = currentPos;
        }

        if (col != null) col.enabled = true;

        // If moved 8 meters, destroy
        if (transform.position.z >= startZ + targetDistance)
        {
            Destroy(gameObject);
        }
    }
}
