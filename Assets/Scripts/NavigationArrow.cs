using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NavigationArrow : MonoBehaviour
{
    private GameObject arrowRoot;
    private GameObject targetTaxi;
    private GameObject targetPassenger;
    private Vector3 targetDestination = new Vector3(45.5f, 0f, 202.9f);

    private List<Material> instantiatedMaterials = new List<Material>();
    private CharacterController controller;

    // UI Compass elements
    private GameObject uiCompassRoot;
    private RectTransform uiArrowContainer;
    private Texture2D uiCircleTexture;
    private Sprite uiCircleSprite;
    private Texture2D uiTipTexture;
    private Sprite uiTipSprite;
    private Text uiDistanceText;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Create the arrow parent
        arrowRoot = new GameObject("NavigationArrowRoot");
        arrowRoot.transform.SetParent(transform, false);
        // Position it slightly off the ground initially
        arrowRoot.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        // Find Unlit/Color shader for flat cartoon colors
        Shader unlitShader = Shader.Find("Unlit/Color");

        // 1. Black Circular Border (Cylinder scaled flat)
        GameObject borderDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(borderDisk.GetComponent<Collider>());
        borderDisk.transform.SetParent(arrowRoot.transform, false);
        borderDisk.transform.localScale = new Vector3(1.1f, 0.001f, 1.1f);
        borderDisk.transform.localPosition = new Vector3(0f, 0f, 0f);
        SetRendererProperties(borderDisk, Color.black, unlitShader);

        // 2. Yellow Inner Circle (Cylinder scaled flat)
        GameObject yellowDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(yellowDisk.GetComponent<Collider>());
        yellowDisk.transform.SetParent(arrowRoot.transform, false);
        yellowDisk.transform.localScale = new Vector3(1.0f, 0.001f, 1.0f);
        yellowDisk.transform.localPosition = new Vector3(0f, 0.003f, 0f); // offset up slightly
        SetRendererProperties(yellowDisk, new Color(1.0f, 0.88f, 0.0f), unlitShader);

        // 3. Black Arrow Shaft (Cube)
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(shaft.GetComponent<Collider>());
        shaft.transform.SetParent(arrowRoot.transform, false);
        shaft.transform.localScale = new Vector3(0.12f, 0.002f, 0.35f);
        shaft.transform.localPosition = new Vector3(0f, 0.006f, -0.1f); // offset up and back
        SetRendererProperties(shaft, Color.black, unlitShader);

        // 4. Black Arrow Tip (Procedural Triangle Mesh)
        GameObject tip = new GameObject("ArrowTip");
        tip.transform.SetParent(arrowRoot.transform, false);
        tip.transform.localPosition = new Vector3(0f, 0.006f, 0.12f);
        
        MeshFilter filter = tip.AddComponent<MeshFilter>();
        triangleMesh = CreateTriangleMesh();
        filter.mesh = triangleMesh;
        
        tip.AddComponent<MeshRenderer>();
        SetRendererProperties(tip, Color.black, unlitShader);

        // Find the player's taxi
        targetTaxi = GameObject.Find("Vehicle_Taxi (9)");

        // Try creating UI Compass
        CreateUICompass();
    }

    private Mesh triangleMesh;

    Mesh CreateTriangleMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowTipTriangle";
        
        Vector3[] vertices = new Vector3[3]
        {
            new Vector3(-0.16f, 0f, -0.10f), // back left
            new Vector3(0f, 0f, 0.18f),     // peak (pointing forward)
            new Vector3(0.16f, 0f, -0.10f)  // back right
        };
        
        int[] triangles = new int[6]
        {
            0, 1, 2, // top face
            0, 2, 1  // bottom face
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

    void CreateUICompass()
    {
        Debug.Log("[NavigationArrow] CreateUICompass started.");
        GameObject canvasGo = GameObject.Find("MissionCanvas");
        if (canvasGo == null)
        {
            Debug.LogError("[NavigationArrow] MissionCanvas NOT found!");
            return;
        }
        Debug.Log("[NavigationArrow] MissionCanvas found successfully.");

        // Create UI Compass Root
        uiCompassRoot = new GameObject("NavigationUICompass");
        uiCompassRoot.transform.SetParent(canvasGo.transform, false);

        RectTransform rootRect = uiCompassRoot.AddComponent<RectTransform>();
        // Anchor to top-right
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = new Vector2(-30f, -30f); // offset slightly more to fit the larger size
        rootRect.sizeDelta = new Vector2(150f, 150f); // 1.5x larger (was 100f, 100f)

        // 1. Circle Background (Yellow with Black Border)
        GameObject circleGo = new GameObject("CircleBg");
        circleGo.transform.SetParent(uiCompassRoot.transform, false);
        RectTransform circleRect = circleGo.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(0f, 0f);
        circleRect.anchorMax = new Vector2(1f, 1f);
        circleRect.offsetMin = Vector2.zero;
        circleRect.offsetMax = Vector2.zero;

        Image circleImage = circleGo.AddComponent<Image>();
        // Generate procedural circular texture
        uiCircleTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dx = x - 64f;
                float dy = y - 64f;
                float distSq = dx * dx + dy * dy;
                if (distSq <= 64f * 64f)
                {
                    if (distSq >= 56f * 56f) // border width of 8 pixels
                    {
                        uiCircleTexture.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        uiCircleTexture.SetPixel(x, y, new Color(1.0f, 0.88f, 0.0f)); // yellow
                    }
                }
                else
                {
                    uiCircleTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        uiCircleTexture.Apply();
        uiCircleSprite = Sprite.Create(uiCircleTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        circleImage.sprite = uiCircleSprite;

        // 2. Arrow Container (for rotation)
        GameObject containerGo = new GameObject("ArrowContainer");
        containerGo.transform.SetParent(uiCompassRoot.transform, false);
        uiArrowContainer = containerGo.AddComponent<RectTransform>();
        uiArrowContainer.anchorMin = new Vector2(0f, 0f);
        uiArrowContainer.anchorMax = new Vector2(1f, 1f);
        uiArrowContainer.offsetMin = Vector2.zero;
        uiArrowContainer.offsetMax = Vector2.zero;

        // 3. Arrow Shaft (scaled 1.5x)
        GameObject shaftGo = new GameObject("ArrowShaft");
        shaftGo.transform.SetParent(uiArrowContainer.transform, false);
        RectTransform shaftRect = shaftGo.AddComponent<RectTransform>();
        shaftRect.anchorMin = new Vector2(0.5f, 0.5f);
        shaftRect.anchorMax = new Vector2(0.5f, 0.5f);
        shaftRect.pivot = new Vector2(0.5f, 0.5f);
        shaftRect.anchoredPosition = new Vector2(0f, -12f); // was -8f
        shaftRect.sizeDelta = new Vector2(24f, 45f); // was 16f, 30f
        Image shaftImage = shaftGo.AddComponent<Image>();
        shaftImage.color = Color.black;

        // 4. Arrow Tip (scaled 1.5x)
        GameObject tipGo = new GameObject("ArrowTip");
        tipGo.transform.SetParent(uiArrowContainer.transform, false);
        RectTransform tipRect = tipGo.AddComponent<RectTransform>();
        tipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipRect.pivot = new Vector2(0.5f, 0.5f);
        tipRect.anchoredPosition = new Vector2(0f, 24f); // was 16f
        tipRect.sizeDelta = new Vector2(57f, 39f); // was 38f, 26f
        Image tipImage = tipGo.AddComponent<Image>();
        
        uiTipTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (y >= 4 && y <= 28)
                {
                    float progress = (y - 4f) / 24f; // 0 at base, 1 at peak
                    float halfWidth = 16f * (1f - progress);
                    if (Mathf.Abs(x - 16f) <= halfWidth)
                    {
                        uiTipTexture.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        uiTipTexture.SetPixel(x, y, Color.clear);
                    }
                }
                else
                {
                    uiTipTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        uiTipTexture.Apply();
        uiTipSprite = Sprite.Create(uiTipTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        tipImage.sprite = uiTipSprite;

        // 5. Distance Text below compass (scaled 1.5x)
        GameObject textGo = new GameObject("DistanceText");
        textGo.transform.SetParent(uiCompassRoot.transform, false);
        
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0.5f, 1f); // top center pivot
        textRect.anchoredPosition = new Vector2(0f, -7.5f); // was -5f
        textRect.sizeDelta = new Vector2(225f, 45f); // was 150f, 30f
        
        uiDistanceText = textGo.AddComponent<Text>();
        
        // Font setup
        if (MissionManager.Instance != null && MissionManager.Instance.CustomFont != null)
        {
            uiDistanceText.font = MissionManager.Instance.CustomFont;
        }
        else
        {
            uiDistanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        uiDistanceText.fontSize = 27; // was 18
        uiDistanceText.color = Color.white;
        uiDistanceText.alignment = TextAnchor.UpperCenter;
        
        Shadow shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        // Hide by default
        uiCompassRoot.SetActive(false);
        Debug.Log("[NavigationArrow] CreateUICompass completed successfully. uiCompassRoot created and set inactive.");
    }

    void SetRendererProperties(GameObject go, Color color, Shader unlitShader)
    {
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            // Clone default material to be pipeline compatible
            Material mat = rend.material;
            
            // Set unlit shader if found, otherwise falls back to standard/URP default
            if (unlitShader != null)
            {
                mat.shader = unlitShader;
            }
            
            mat.color = color;
            instantiatedMaterials.Add(mat);
            
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }
    }

    void LateUpdate()
    {
        if (arrowRoot == null) return;

        if (uiCompassRoot == null)
        {
            CreateUICompass();
        }

        // If mission is not active yet, hide everything and wait
        if (MissionManager.Instance != null && !MissionManager.Instance.IsMissionActive)
        {
            if (arrowRoot.activeSelf) arrowRoot.SetActive(false);
            if (uiCompassRoot != null && uiCompassRoot.activeSelf) uiCompassRoot.SetActive(false);
            return;
        }

        // Determine current target based on MissionManager state
        Vector3 targetPos = Vector3.zero;
        bool hasTarget = false;

        if (MissionManager.Instance != null)
        {
            int state = MissionManager.Instance.currentState;

            if (state == 0) // Mission 1: Find Taxi
            {
                if (targetTaxi == null) targetTaxi = GameObject.Find("Vehicle_Taxi (9)");
                if (targetTaxi != null)
                {
                    targetPos = targetTaxi.transform.position;
                    hasTarget = true;
                }
            }
            else if (state == 1) // Mission 2: Find Passenger
            {
                if (targetPassenger == null)
                {
                    targetPassenger = GameObject.Find("TaxiPassengerNPC");
                    if (targetPassenger == null)
                    {
                        // Fallback to searching active passenger component in scene
                        TaxiPassenger passengerComp = FindObjectOfType<TaxiPassenger>(true);
                        if (passengerComp != null) targetPassenger = passengerComp.gameObject;
                    }
                }

                if (targetPassenger != null && targetPassenger.activeInHierarchy)
                {
                    targetPos = targetPassenger.transform.position;
                    hasTarget = true;
                }
            }
            else if (state == 2) // Mission 3: Bring Passenger Home
            {
                targetPos = targetDestination;
                hasTarget = true;
            }
        }
        else
        {
            // Default behavior if MissionManager is missing: always point to taxi
            if (targetTaxi == null) targetTaxi = GameObject.Find("Vehicle_Taxi (9)");
            if (targetTaxi != null)
            {
                targetPos = targetTaxi.transform.position;
                hasTarget = true;
            }
        }

        bool isDriving = (controller != null && !controller.enabled);

        if (isDriving)
        {
            // Hide 3D floor arrow
            if (arrowRoot.activeSelf) arrowRoot.SetActive(false);

            // Update UI Compass
            if (hasTarget)
            {
                if (uiCompassRoot != null && !uiCompassRoot.activeSelf) uiCompassRoot.SetActive(true);

                Vector3 direction = targetPos - transform.position;
                direction.y = 0;
                float dist = direction.magnitude;

                // Update Distance Text
                if (uiDistanceText != null)
                {
                    uiDistanceText.text = Mathf.RoundToInt(dist) + "m";
                }

                if (dist > 0.1f && uiArrowContainer != null)
                {
                    // Calculate relative angle to player's forward direction (which aligns with vehicle)
                    float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
                    uiArrowContainer.localRotation = Quaternion.Euler(0f, 0f, -angle);
                }
            }
            else
            {
                if (uiCompassRoot != null && uiCompassRoot.activeSelf) uiCompassRoot.SetActive(false);
            }
        }
        else
        {
            // Hide UI Compass
            if (uiCompassRoot != null && uiCompassRoot.activeSelf) uiCompassRoot.SetActive(false);

            // Update 3D Floor Arrow
            if (hasTarget)
            {
                if (!arrowRoot.activeSelf) arrowRoot.SetActive(true);

                Vector3 direction = targetPos - transform.position;
                direction.y = 0; // Ignore height difference

                float dist = direction.magnitude;
                if (dist > 0.1f)
                {
                    Vector3 dirNormalized = direction / dist;
                    float offsetDist = Mathf.Min(1.8f, dist);
                    
                    // Position arrowRoot in world coordinates relative to the player
                    arrowRoot.transform.position = transform.position + dirNormalized * offsetDist + Vector3.up * 0.05f;
                    arrowRoot.transform.rotation = Quaternion.LookRotation(dirNormalized);
                }
                else
                {
                    // If extremely close, position right at character's center
                    arrowRoot.transform.position = transform.position + Vector3.up * 0.05f;
                }
            }
            else
            {
                if (arrowRoot.activeSelf) arrowRoot.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        if (arrowRoot != null)
        {
            Destroy(arrowRoot);
        }
        if (triangleMesh != null)
        {
            Destroy(triangleMesh);
        }
        if (instantiatedMaterials != null)
        {
            foreach (var mat in instantiatedMaterials)
            {
                if (mat != null) Destroy(mat);
            }
            instantiatedMaterials.Clear();
        }
        if (uiCircleSprite != null) Destroy(uiCircleSprite);
        if (uiCircleTexture != null) Destroy(uiCircleTexture);
        if (uiTipSprite != null) Destroy(uiTipSprite);
        if (uiTipTexture != null) Destroy(uiTipTexture);
        if (uiCompassRoot != null) Destroy(uiCompassRoot);
    }
}
