using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    private Canvas canvas;
    private Text3D missionHeader;
    private Text3D text1;
    private Text3D text2;
    private Text3D text3;
    private Text3D text4;

    private Font customFont;
    public Font CustomFont => customFont;
    private GameObject passengerNpc;

    public bool IsMissionActive { get; private set; } = false;
    private GameObject successPopupContainer;
    private GameObject failPopupContainer;
    private Text uiTimerText;
    private float missionTimer = 120.0f;

    // Success popup text and strikethrough lines
    private Text successText;
    private Text rewardText;
    private Image successFrontLine;
    private System.Collections.Generic.List<Image> successShadowLines = new System.Collections.Generic.List<Image>();
    private Image rewardFrontLine;
    private System.Collections.Generic.List<Image> rewardShadowLines = new System.Collections.Generic.List<Image>();

    // 0: Start, 1: M1 Clear (M2 Active), 2: M2 Clear (M3 Active), 3: M3 Clear (M4 Active), 4: M4 Clear (All Done)
    public int currentState = 0; 

    private GameObject destinationGuide = null;
    private Vector3 targetDestination = new Vector3(45.5f, 0f, 202.9f);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Cache the passenger NPC while it is active at start
        passengerNpc = GameObject.Find("TaxiPassengerNPC");
        if (passengerNpc == null)
        {
            TaxiPassenger tp = FindObjectOfType<TaxiPassenger>(true);
            if (tp != null)
            {
                passengerNpc = tp.gameObject;
            }
        }

        CreateUI();
    }

    void Start()
    {
        IsMissionActive = false;
        UpdateUI();

        // Spawn Traffic Manager to control city traffic looping
        GameObject trafficGo = new GameObject("TrafficManager");
        trafficGo.AddComponent<TrafficManager>();

        // Start delayed mission start coroutine
        StartCoroutine(DelayMissionStart());
    }

    private Coroutine activeIntroCoroutine = null;

    private IEnumerator DelayMissionStart()
    {
        Debug.Log("[MissionManager] DelayMissionStart started. Waiting 20 seconds...");
        yield return new WaitForSeconds(20.0f);

        missionTimer = 120.0f; // Reset timer to 120 seconds
        
        // Auto-detect if player is already inside the Taxi to advance state
        ThirdPersonCharacterController player = FindObjectOfType<ThirdPersonCharacterController>();
        if (player != null && player.transform.parent != null && player.transform.parent.name.Contains("Vehicle_Taxi"))
        {
            currentState = 1; // Auto advance to Mission 2
        }
        else
        {
            currentState = 0;
        }

        // Start the intro animation for the MISSION! header
        activeIntroCoroutine = StartCoroutine(AnimateMissionHeader());

        Debug.Log("[MissionManager] Mission activated, UI transition started. State: " + currentState);
    }

    private IEnumerator AnimateMissionHeader()
    {
        // Disable general UI updates during animation to avoid layout overwrite
        IsMissionActive = false;

        // Clear all text lines first
        missionHeader.text = "";
        text1.text = "";
        text2.text = "";
        text3.text = "";
        text4.text = "";

        // Set the active text to be animated
        missionHeader.text = "MISSION!";
        missionHeader.color = Color.red;

        RectTransform rect = missionHeader.containerRect;
        if (rect == null)
        {
            Debug.LogError("[MissionManager] containerRect is null for missionHeader!");
            IsMissionActive = true;
            UpdateUI();
            yield break;
        }

        // Force canvas to update layout to get correct preferredWidth
        Canvas.ForceUpdateCanvases();

        // Get preferredWidth of frontText to calculate center alignment offset
        float preferredWidth = missionHeader.frontText != null ? missionHeader.frontText.preferredWidth : 200f;

        // Get canvas size
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        float canvasWidth = canvasRect != null ? canvasRect.rect.width : 800f;
        float canvasHeight = canvasRect != null ? canvasRect.rect.height : 600f;

        float startScale = 4.0f; // 1.5x of size 80 is equivalent to scale 4.0 over base 30

        // Calculate start position relative to top-left anchor (0, 1) so that the text center is at screen center
        Vector2 startPos = new Vector2(canvasWidth / 2f - (preferredWidth / 2f) * startScale, -canvasHeight / 2f + 25f * startScale + 90f);
        Vector2 targetPos = new Vector2(20f, -2f); // Shifted up further

        // Set start state (centered and large)
        rect.localScale = new Vector3(startScale, startScale, startScale);
        rect.anchoredPosition = startPos;

        // 1. Show in center and blink 3 times over 2.0 seconds
        yield return new WaitForSeconds(0.3f);
        missionHeader.text = "";
        yield return new WaitForSeconds(0.15f);
        missionHeader.text = "MISSION!";
        yield return new WaitForSeconds(0.3f);
        missionHeader.text = "";
        yield return new WaitForSeconds(0.15f);
        missionHeader.text = "MISSION!";
        yield return new WaitForSeconds(0.3f);
        missionHeader.text = "";
        yield return new WaitForSeconds(0.15f);
        missionHeader.text = "MISSION!";
        yield return new WaitForSeconds(0.65f);

        // 2. Shrink and move to top-left header slot over 1.5 seconds
        float duration = 1.5f;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float currentScale = Mathf.Lerp(startScale, 1.0f, t);
            rect.localScale = new Vector3(currentScale, currentScale, currentScale);
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // Ensure exactly at destination
        rect.localScale = Vector3.one;
        rect.anchoredPosition = targetPos;

        // 3. 0.5-second delay after settling
        yield return new WaitForSeconds(0.5f);

        // Enable normal UI updates (which shows the detailed missions shifted down)
        IsMissionActive = true;
        UpdateUI();
    }

    private IEnumerator FailSequence()
    {
        IsMissionActive = false;

        if (activeIntroCoroutine != null)
        {
            StopCoroutine(activeIntroCoroutine);
            activeIntroCoroutine = null;
        }
        
        if (passengerNpc != null)
        {
            passengerNpc.SetActive(false);
        }

        if (destinationGuide != null)
        {
            Destroy(destinationGuide);
            destinationGuide = null;
        }

        if (missionHeader != null) missionHeader.text = "";
        text1.text = "";
        text2.text = "";
        text3.text = "";
        text4.text = "";
        ResetAllStrikethroughs();

        if (uiTimerText != null)
        {
            uiTimerText.gameObject.SetActive(false);
        }

        // Show the FAILED! popup in the center
        if (failPopupContainer != null)
        {
            failPopupContainer.SetActive(true);
        }

        yield return new WaitForSeconds(4.0f);

        if (failPopupContainer != null)
        {
            failPopupContainer.SetActive(false);
        }

        // Reset state and trigger delayed restart
        currentState = 0;
        StartCoroutine(DelayMissionStart());
    }

    void Update()
    {
        // Handle Timer tick down
        if (IsMissionActive && currentState < 4)
        {
            missionTimer -= Time.deltaTime;
            if (missionTimer <= 0f)
            {
                missionTimer = 0f;
                StartCoroutine(FailSequence());
            }

            if (uiTimerText != null)
            {
                if (!uiTimerText.gameObject.activeSelf) uiTimerText.gameObject.SetActive(true);
                
                if (missionTimer <= 30.0f)
                {
                    uiTimerText.color = Color.red;
                    uiTimerText.text = missionTimer.ToString("00.00");
                }
                else
                {
                    uiTimerText.color = Color.white;
                    int totalSeconds = Mathf.CeilToInt(missionTimer);
                    int minutes = totalSeconds / 60;
                    int seconds = totalSeconds % 60;
                    uiTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                }
            }
        }
        else
        {
            if (uiTimerText != null && uiTimerText.gameObject.activeSelf)
            {
                uiTimerText.gameObject.SetActive(false);
            }
        }

        // If in Mission 3, check distance between Taxi and destination
        if (currentState == 2 && IsMissionActive)
        {
            ThirdPersonCharacterController player = FindObjectOfType<ThirdPersonCharacterController>();
            if (player != null && player.transform.parent != null)
            {
                Transform taxi = player.transform.parent;
                if (taxi.name.Contains("Vehicle_Taxi"))
                {
                    // Check horizontal distance (ignoring height differences)
                    Vector3 taxiPos = taxi.position;
                    Vector3 destPos = targetDestination;
                    taxiPos.y = 0;
                    destPos.y = 0;

                    float dist = Vector3.Distance(taxiPos, destPos);
                    // 3 meter radius complete condition
                    if (dist <= 3.0f)
                    {
                        SetState(3);
                    }
                }
            }
        }
    }

    private void CreateUI()
    {
        // Create Canvas
        GameObject canvasGo = new GameObject("MissionCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGo);

        // Load Default Font safely
        try
        {
            customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (System.Exception)
        {
            customFont = null;
        }

        if (customFont == null)
        {
            GameObject tempGo = new GameObject("TempFontSource");
            Text tempText = tempGo.AddComponent<Text>();
            customFont = tempText.font;
            if (Application.isPlaying)
            {
                Destroy(tempGo);
            }
            else
            {
                DestroyImmediate(tempGo);
            }
        }

        // Create Texts
        missionHeader = CreateText3D(canvasGo.transform, "MissionHeader", -2);
        text1 = CreateText3D(canvasGo.transform, "MissionText1", -52);
        text2 = CreateText3D(canvasGo.transform, "MissionText2", -102);
        text3 = CreateText3D(canvasGo.transform, "MissionText3", -152);
        text4 = CreateText3D(canvasGo.transform, "MissionText4", -202);

        // Create "SUCCESS!" Popup Container
        successPopupContainer = new GameObject("SuccessPopupContainer");
        successPopupContainer.layer = 5; // Set to UI layer
        successPopupContainer.transform.SetParent(canvasGo.transform, false);

        RectTransform successRect = successPopupContainer.AddComponent<RectTransform>();
        successRect.anchorMin = new Vector2(0.5f, 0.5f);
        successRect.anchorMax = new Vector2(0.5f, 0.5f);
        successRect.pivot = new Vector2(0.5f, 0.5f);
        successRect.anchoredPosition = Vector2.zero;
        successRect.sizeDelta = new Vector2(600f, 300f);

        // A. Layered shadows for "SUCCESS!" (Green style, 15 layers)
        for (int i = 15; i >= 1; i--)
        {
            GameObject shadowGo = new GameObject("SuccessPopupShadow_" + i);
            shadowGo.layer = 5;
            shadowGo.transform.SetParent(successPopupContainer.transform, false);

            RectTransform sRect = shadowGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(i * 1.2f, 40f - i * 1.2f);
            sRect.sizeDelta = new Vector2(600f, 120f);

            Text sText = shadowGo.AddComponent<Text>();
            sText.font = customFont;
            sText.fontSize = 80;
            sText.fontStyle = FontStyle.Bold;
            sText.color = Color.black;
            sText.text = "미션성공!";
            sText.alignment = TextAnchor.MiddleCenter;
            sText.horizontalOverflow = HorizontalWrapMode.Overflow;
            sText.verticalOverflow = VerticalWrapMode.Overflow;

            // Create Strikethrough Shadow Line
            GameObject lineShadowGo = new GameObject("SuccessPopupShadowLine_" + i);
            lineShadowGo.layer = 5;
            lineShadowGo.transform.SetParent(shadowGo.transform, false);

            RectTransform lsRect = lineShadowGo.AddComponent<RectTransform>();
            lsRect.anchorMin = new Vector2(0.5f, 0.5f);
            lsRect.anchorMax = new Vector2(0.5f, 0.5f);
            lsRect.pivot = new Vector2(0f, 0.5f);
            lsRect.anchoredPosition = Vector2.zero;
            lsRect.sizeDelta = new Vector2(0f, 6f);

            Image lsImage = lineShadowGo.AddComponent<Image>();
            lsImage.color = Color.black;
            lineShadowGo.SetActive(false);

            successShadowLines.Add(lsImage);
        }

        // B. Foreground "SUCCESS!"
        GameObject successFrontGo = new GameObject("SuccessPopupFront");
        successFrontGo.layer = 5;
        successFrontGo.transform.SetParent(successPopupContainer.transform, false);

        RectTransform successFrontRect = successFrontGo.AddComponent<RectTransform>();
        successFrontRect.anchorMin = new Vector2(0.5f, 0.5f);
        successFrontRect.anchorMax = new Vector2(0.5f, 0.5f);
        successFrontRect.pivot = new Vector2(0.5f, 0.5f);
        successFrontRect.anchoredPosition = new Vector2(0f, 40f);
        successFrontRect.sizeDelta = new Vector2(600f, 120f);

        successText = successFrontGo.AddComponent<Text>();
        successText.font = customFont;
        successText.fontSize = 80;
        successText.fontStyle = FontStyle.Bold;
        successText.color = Color.green; // Green text
        successText.text = "미션성공!";
        successText.alignment = TextAnchor.MiddleCenter;
        successText.horizontalOverflow = HorizontalWrapMode.Overflow;
        successText.verticalOverflow = VerticalWrapMode.Overflow;

        // Create Strikethrough Foreground Line
        GameObject lineFrontGo = new GameObject("SuccessPopupFrontLine");
        lineFrontGo.layer = 5;
        lineFrontGo.transform.SetParent(successFrontGo.transform, false);

        RectTransform lfRect = lineFrontGo.AddComponent<RectTransform>();
        lfRect.anchorMin = new Vector2(0.5f, 0.5f);
        lfRect.anchorMax = new Vector2(0.5f, 0.5f);
        lfRect.pivot = new Vector2(0f, 0.5f);
        lfRect.anchoredPosition = Vector2.zero;
        lfRect.sizeDelta = new Vector2(0f, 6f);

        successFrontLine = lineFrontGo.AddComponent<Image>();
        successFrontLine.color = Color.green;
        lineFrontGo.SetActive(false);

        // C. Layered shadows for "+3$" (Green style, 10 layers)
        for (int i = 10; i >= 1; i--)
        {
            GameObject shadowGo = new GameObject("RewardPopupShadow_" + i);
            shadowGo.layer = 5;
            shadowGo.transform.SetParent(successPopupContainer.transform, false);

            RectTransform sRect = shadowGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(i * 1.2f, -60f - i * 1.2f);
            sRect.sizeDelta = new Vector2(600f, 80f);

            Text sText = shadowGo.AddComponent<Text>();
            sText.font = customFont;
            sText.fontSize = 48; // Medium size
            sText.fontStyle = FontStyle.Bold;
            sText.color = Color.black;
            sText.text = "+3$";
            sText.alignment = TextAnchor.MiddleCenter;
            sText.horizontalOverflow = HorizontalWrapMode.Overflow;
            sText.verticalOverflow = VerticalWrapMode.Overflow;

            // Create Reward Strikethrough Shadow Line
            GameObject lineShadowGo = new GameObject("RewardPopupShadowLine_" + i);
            lineShadowGo.layer = 5;
            lineShadowGo.transform.SetParent(shadowGo.transform, false);

            RectTransform lsRect = lineShadowGo.AddComponent<RectTransform>();
            lsRect.anchorMin = new Vector2(0.5f, 0.5f);
            lsRect.anchorMax = new Vector2(0.5f, 0.5f);
            lsRect.pivot = new Vector2(0f, 0.5f);
            lsRect.anchoredPosition = Vector2.zero;
            lsRect.sizeDelta = new Vector2(0f, 5f);

            Image lsImage = lineShadowGo.AddComponent<Image>();
            lsImage.color = Color.black;
            lineShadowGo.SetActive(false);

            rewardShadowLines.Add(lsImage);
        }

        // D. Foreground "+3$"
        GameObject rewardFrontGo = new GameObject("RewardPopupFront");
        rewardFrontGo.layer = 5;
        rewardFrontGo.transform.SetParent(successPopupContainer.transform, false);

        RectTransform rewardFrontRect = rewardFrontGo.AddComponent<RectTransform>();
        rewardFrontRect.anchorMin = new Vector2(0.5f, 0.5f);
        rewardFrontRect.anchorMax = new Vector2(0.5f, 0.5f);
        rewardFrontRect.pivot = new Vector2(0.5f, 0.5f);
        rewardFrontRect.anchoredPosition = new Vector2(0f, -60f);
        rewardFrontRect.sizeDelta = new Vector2(600f, 80f);

        rewardText = rewardFrontGo.AddComponent<Text>();
        rewardText.font = customFont;
        rewardText.fontSize = 48; // Medium size
        rewardText.fontStyle = FontStyle.Bold;
        rewardText.color = Color.green; // Green text
        rewardText.text = "+3$";
        rewardText.alignment = TextAnchor.MiddleCenter;
        rewardText.horizontalOverflow = HorizontalWrapMode.Overflow;
        rewardText.verticalOverflow = VerticalWrapMode.Overflow;

        // Create Reward Strikethrough Foreground Line
        GameObject rewardLineFrontGo = new GameObject("RewardPopupFrontLine");
        rewardLineFrontGo.layer = 5;
        rewardLineFrontGo.transform.SetParent(rewardFrontGo.transform, false);

        RectTransform rlfRect = rewardLineFrontGo.AddComponent<RectTransform>();
        rlfRect.anchorMin = new Vector2(0.5f, 0.5f);
        rlfRect.anchorMax = new Vector2(0.5f, 0.5f);
        rlfRect.pivot = new Vector2(0f, 0.5f);
        rlfRect.anchoredPosition = Vector2.zero;
        rlfRect.sizeDelta = new Vector2(0f, 5f);

        rewardFrontLine = rewardLineFrontGo.AddComponent<Image>();
        rewardFrontLine.color = Color.green;
        rewardLineFrontGo.SetActive(false);

        successPopupContainer.SetActive(false); // Hide container initially

        // Create "FAILED!" Popup Container
        failPopupContainer = new GameObject("FailPopupContainer");
        failPopupContainer.layer = 5; // Set to UI layer
        failPopupContainer.transform.SetParent(canvasGo.transform, false);

        RectTransform failRect = failPopupContainer.AddComponent<RectTransform>();
        failRect.anchorMin = new Vector2(0.5f, 0.5f);
        failRect.anchorMax = new Vector2(0.5f, 0.5f);
        failRect.pivot = new Vector2(0.5f, 0.5f);
        failRect.anchoredPosition = Vector2.zero;
        failRect.sizeDelta = new Vector2(600f, 150f);

        // Layered shadows for "FAILED!" (Red style, 15 layers)
        for (int i = 15; i >= 1; i--)
        {
            GameObject shadowGo = new GameObject("FailPopupShadow_" + i);
            shadowGo.layer = 5;
            shadowGo.transform.SetParent(failPopupContainer.transform, false);

            RectTransform sRect = shadowGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(i * 1.2f, -i * 1.2f);
            sRect.sizeDelta = new Vector2(600f, 150f);

            Text sText = shadowGo.AddComponent<Text>();
            sText.font = customFont;
            sText.fontSize = 80;
            sText.fontStyle = FontStyle.Bold;
            sText.color = Color.black;
            sText.text = "실패!";
            sText.alignment = TextAnchor.MiddleCenter;
            sText.horizontalOverflow = HorizontalWrapMode.Overflow;
            sText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        // Foreground "FAILED!"
        GameObject failFrontGo = new GameObject("FailPopupFront");
        failFrontGo.layer = 5;
        failFrontGo.transform.SetParent(failPopupContainer.transform, false);

        RectTransform failFrontRect = failFrontGo.AddComponent<RectTransform>();
        failFrontRect.anchorMin = new Vector2(0.5f, 0.5f);
        failFrontRect.anchorMax = new Vector2(0.5f, 0.5f);
        failFrontRect.pivot = new Vector2(0.5f, 0.5f);
        failFrontRect.anchoredPosition = Vector2.zero;
        failFrontRect.sizeDelta = new Vector2(600f, 150f);

        Text failText = failFrontGo.AddComponent<Text>();
        failText.font = customFont;
        failText.fontSize = 80;
        failText.fontStyle = FontStyle.Bold;
        failText.color = Color.red; // Red text
        failText.text = "실패!";
        failText.alignment = TextAnchor.MiddleCenter;
        failText.horizontalOverflow = HorizontalWrapMode.Overflow;
        failText.verticalOverflow = VerticalWrapMode.Overflow;

        failPopupContainer.SetActive(false); // Hide container initially

        // Create Timer Text (Top-Center)
        GameObject timerGo = new GameObject("TimerText");
        timerGo.layer = 5; // Set to UI layer
        timerGo.transform.SetParent(canvasGo.transform, false);

        RectTransform timerRect = timerGo.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0f, -20f);
        timerRect.sizeDelta = new Vector2(200f, 50f);

        uiTimerText = timerGo.AddComponent<Text>();
        uiTimerText.font = customFont;
        uiTimerText.fontSize = 36;
        uiTimerText.fontStyle = FontStyle.Bold;
        uiTimerText.color = Color.white; // White text
        uiTimerText.alignment = TextAnchor.UpperCenter;
        uiTimerText.text = "";

        Shadow timerShadow = timerGo.AddComponent<Shadow>();
        timerShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        timerShadow.effectDistance = new Vector2(1.5f, -1.5f);

        timerGo.SetActive(false); // Hide initially
    }

    private class Text3D
    {
        public RectTransform containerRect;
        public Text frontText;
        public System.Collections.Generic.List<Text> shadowTexts = new System.Collections.Generic.List<Text>();
        public Image frontLine;
        public System.Collections.Generic.List<Image> shadowLines = new System.Collections.Generic.List<Image>();
        public bool isCompleted = false;
        public Coroutine strikethroughCoroutine = null;

        public string text
        {
            get => frontText != null ? frontText.text : "";
            set
            {
                if (frontText != null) frontText.text = value;
                foreach (var st in shadowTexts)
                {
                    if (st != null) st.text = value;
                }
            }
        }

        public Color color
        {
            get => frontText != null ? frontText.color : Color.white;
            set
            {
                if (frontText != null) frontText.color = value;
            }
        }
    }

    private Text3D CreateText3D(Transform parent, string name, float anchoredY)
    {
        Text3D text3D = new Text3D();

        // Create Container
        GameObject containerGo = new GameObject(name + "_Container");
        containerGo.layer = 5;
        containerGo.transform.SetParent(parent, false);

        RectTransform rect = containerGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, anchoredY);
        rect.sizeDelta = new Vector2(700, 50);

        // 1. Create 6 layered shadow texts underneath (shifted diagonally)
        for (int i = 6; i >= 1; i--)
        {
            GameObject shadowGo = new GameObject(name + "_Shadow_" + i);
            shadowGo.layer = 5;
            shadowGo.transform.SetParent(containerGo.transform, false);

            RectTransform sRect = shadowGo.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.offsetMin = new Vector2(i * 1.0f, -i * 1.0f);
            sRect.offsetMax = new Vector2(i * 1.0f, -i * 1.0f);

            Text sText = shadowGo.AddComponent<Text>();
            sText.font = customFont;
            sText.fontSize = 30; // 1.5x bigger
            sText.fontStyle = FontStyle.Bold; // thick/bold
            sText.color = Color.black;
            sText.text = "";
            sText.alignment = TextAnchor.MiddleLeft;
            sText.horizontalOverflow = HorizontalWrapMode.Overflow;
            sText.verticalOverflow = VerticalWrapMode.Overflow;

            text3D.shadowTexts.Add(sText);

            // Create Shadow Strikethrough Line
            GameObject shadowLineGo = new GameObject(name + "_ShadowLine_" + i);
            shadowLineGo.layer = 5;
            shadowLineGo.transform.SetParent(shadowGo.transform, false);

            RectTransform slRect = shadowLineGo.AddComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0f, 0.5f);
            slRect.anchorMax = new Vector2(0f, 0.5f);
            slRect.pivot = new Vector2(0f, 0.5f);
            slRect.anchoredPosition = new Vector2(0f, -2f);
            slRect.sizeDelta = new Vector2(0f, 4f);

            Image slImage = shadowLineGo.AddComponent<Image>();
            slImage.color = Color.black;
            shadowLineGo.SetActive(false);

            text3D.shadowLines.Add(slImage);
        }

        // 2. Create Foreground text
        GameObject frontGo = new GameObject(name + "_Front");
        frontGo.layer = 5;
        frontGo.transform.SetParent(containerGo.transform, false);

        RectTransform frontRect = frontGo.AddComponent<RectTransform>();
        frontRect.anchorMin = Vector2.zero;
        frontRect.anchorMax = Vector2.one;
        frontRect.offsetMin = Vector2.zero;
        frontRect.offsetMax = Vector2.zero;

        Text frontText = frontGo.AddComponent<Text>();
        frontText.font = customFont;
        frontText.fontSize = 30; // 1.5x bigger
        frontText.fontStyle = FontStyle.Bold; // thick/bold
        frontText.color = Color.white;
        frontText.text = "";
        frontText.alignment = TextAnchor.MiddleLeft;
        frontText.horizontalOverflow = HorizontalWrapMode.Overflow;
        frontText.verticalOverflow = VerticalWrapMode.Overflow;

        text3D.frontText = frontText;
        text3D.containerRect = rect;

        // Create Foreground Strikethrough Line
        GameObject frontLineGo = new GameObject(name + "_FrontLine");
        frontLineGo.layer = 5;
        frontLineGo.transform.SetParent(frontGo.transform, false);

        RectTransform flRect = frontLineGo.AddComponent<RectTransform>();
        flRect.anchorMin = new Vector2(0f, 0.5f);
        flRect.anchorMax = new Vector2(0f, 0.5f);
        flRect.pivot = new Vector2(0f, 0.5f);
        flRect.anchoredPosition = new Vector2(0f, -2f);
        flRect.sizeDelta = new Vector2(0f, 4f);

        Image flImage = frontLineGo.AddComponent<Image>();
        flImage.color = Color.green;
        frontLineGo.SetActive(false);

        text3D.frontLine = flImage;

        return text3D;
    }

    private void SetStrikethrough(Text3D text3D, bool completed, bool animate)
    {
        if (text3D == null) return;
        if (text3D.isCompleted == completed) return;
        text3D.isCompleted = completed;

        if (text3D.strikethroughCoroutine != null)
        {
            StopCoroutine(text3D.strikethroughCoroutine);
            text3D.strikethroughCoroutine = null;
        }

        if (completed)
        {
            if (animate && Application.isPlaying)
            {
                text3D.strikethroughCoroutine = StartCoroutine(AnimateStrikethrough(text3D));
            }
            else
            {
                SetLineProgress(text3D, 1f);
            }
        }
        else
        {
            SetLineProgress(text3D, 0f);
        }
    }

    private void SetLineProgress(Text3D text3D, float progress)
    {
        float targetWidth = 0f;
        float extraPadding = 30f; // 30 pixels total extra length (e.g. 15px on left, 15px on right)
        
        if (text3D.frontText != null)
        {
            Canvas.ForceUpdateCanvases();
            targetWidth = (text3D.frontText.preferredWidth + extraPadding) * progress;
        }

        float lineOffset = -(extraPadding / 2f) * progress;

        if (text3D.frontLine != null)
        {
            text3D.frontLine.rectTransform.anchoredPosition = new Vector2(lineOffset, -2f);
            text3D.frontLine.rectTransform.sizeDelta = new Vector2(targetWidth, 4f);
            text3D.frontLine.gameObject.SetActive(progress > 0f);
        }

        foreach (var sl in text3D.shadowLines)
        {
            if (sl != null)
            {
                sl.rectTransform.anchoredPosition = new Vector2(lineOffset, -2f);
                sl.rectTransform.sizeDelta = new Vector2(targetWidth, 4f);
                sl.gameObject.SetActive(progress > 0f);
            }
        }
    }

    private System.Collections.IEnumerator AnimateStrikethrough(Text3D text3D)
    {
        float duration = 1.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // Ease out quad
            progress = progress * (2f - progress);
            SetLineProgress(text3D, progress);
            yield return null;
        }

        SetLineProgress(text3D, 1f);
        text3D.strikethroughCoroutine = null;
    }

    private void ResetAllStrikethroughs()
    {
        SetStrikethrough(text1, false, false);
        SetStrikethrough(text2, false, false);
        SetStrikethrough(text3, false, false);
        SetStrikethrough(text4, false, false);
    }

    private void ResetSuccessStrikethroughs()
    {
        if (successFrontLine != null)
        {
            successFrontLine.rectTransform.sizeDelta = new Vector2(0f, 6f);
            successFrontLine.gameObject.SetActive(false);
        }
        foreach (var sl in successShadowLines)
        {
            if (sl != null)
            {
                sl.rectTransform.sizeDelta = new Vector2(0f, 6f);
                sl.gameObject.SetActive(false);
            }
        }
        if (rewardFrontLine != null)
        {
            rewardFrontLine.rectTransform.sizeDelta = new Vector2(0f, 5f);
            rewardFrontLine.gameObject.SetActive(false);
        }
        foreach (var rl in rewardShadowLines)
        {
            if (rl != null)
            {
                rl.rectTransform.sizeDelta = new Vector2(0f, 5f);
                rl.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator AnimateSuccessStrikethroughs()
    {
        float duration = 1.6f;
        float elapsed = 0f;

        Canvas.ForceUpdateCanvases();

        float extraPadding = 40f; // 40 pixels total extra length (20px on left, 20px on right)
        float successPrefWidth = successText != null ? (successText.preferredWidth + extraPadding) : 240f;
        float rewardPrefWidth = rewardText != null ? (rewardText.preferredWidth + extraPadding) : 140f;

        if (successFrontLine != null)
        {
            successFrontLine.rectTransform.anchoredPosition = new Vector2(-successPrefWidth / 2f, 0f);
        }
        foreach (var sl in successShadowLines)
        {
            if (sl != null) sl.rectTransform.anchoredPosition = new Vector2(-successPrefWidth / 2f, 0f);
        }

        if (rewardFrontLine != null)
        {
            rewardFrontLine.rectTransform.anchoredPosition = new Vector2(-rewardPrefWidth / 2f, 0f);
        }
        foreach (var rl in rewardShadowLines)
        {
            if (rl != null) rl.rectTransform.anchoredPosition = new Vector2(-rewardPrefWidth / 2f, 0f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * (2f - progress);

            float currentSuccessWidth = successPrefWidth * progress;
            float currentRewardWidth = rewardPrefWidth * progress;

            if (successFrontLine != null)
            {
                successFrontLine.rectTransform.sizeDelta = new Vector2(currentSuccessWidth, 6f);
                successFrontLine.gameObject.SetActive(progress > 0f);
            }
            foreach (var sl in successShadowLines)
            {
                if (sl != null)
                {
                    sl.rectTransform.sizeDelta = new Vector2(currentSuccessWidth, 6f);
                    sl.gameObject.SetActive(progress > 0f);
                }
            }

            if (rewardFrontLine != null)
            {
                rewardFrontLine.rectTransform.sizeDelta = new Vector2(currentRewardWidth, 5f);
                rewardFrontLine.gameObject.SetActive(progress > 0f);
            }
            foreach (var rl in rewardShadowLines)
            {
                if (rl != null)
                {
                    rl.rectTransform.sizeDelta = new Vector2(currentRewardWidth, 5f);
                    rl.gameObject.SetActive(progress > 0f);
                }
            }

            yield return null;
        }

        if (successFrontLine != null) successFrontLine.rectTransform.sizeDelta = new Vector2(successPrefWidth, 6f);
        foreach (var sl in successShadowLines)
        {
            if (sl != null) sl.rectTransform.sizeDelta = new Vector2(successPrefWidth, 6f);
        }
        if (rewardFrontLine != null) rewardFrontLine.rectTransform.sizeDelta = new Vector2(rewardPrefWidth, 5f);
        foreach (var rl in rewardShadowLines)
        {
            if (rl != null) rl.rectTransform.sizeDelta = new Vector2(rewardPrefWidth, 5f);
        }
    }

    public void UpdateUI()
    {
        // Handle Passenger NPC visibility based on state using cached reference
        if (passengerNpc != null)
        {
            passengerNpc.SetActive(currentState == 1 && IsMissionActive);
        }

        if (!IsMissionActive)
        {
            if (missionHeader != null) missionHeader.text = "";
            text1.text = "";
            text2.text = "";
            text3.text = "";
            text4.text = "";
            ResetAllStrikethroughs();
            return;
        }

        if (missionHeader != null)
        {
        missionHeader.text = "MISSION!";
        missionHeader.color = Color.red;
        }

        if (currentState == 0)
        {
            text1.text = "1. 택시에 타라";
            text1.color = Color.white;
            text2.text = "";
            text3.text = "";
            text4.text = "";
        }
        else if (currentState == 1)
        {
            text1.text = "1. 택시에 타라";
            text1.color = Color.green;
            text2.text = "2. 승객을 찾아라";
            text2.color = Color.white;
            text3.text = "";
            text4.text = "";
        }
        else if (currentState == 2)
        {
            text1.text = "1. 택시에 타라";
            text1.color = Color.green;
            text2.text = "2. 승객을 찾아라";
            text2.color = Color.green;
            text3.text = "3. 목적지로 이동해라";
            text3.color = Color.white;
            text4.text = "";

            // Spawn Destination visual guide
            if (destinationGuide == null && Application.isPlaying)
            {
                destinationGuide = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                destinationGuide.name = "DestinationGuide";
                destinationGuide.transform.position = new Vector3(targetDestination.x, 1.0f, targetDestination.z);
                // Diameter = 6m (Radius = 3m)
                destinationGuide.transform.localScale = new Vector3(6.0f, 1.0f, 6.0f);
                
                // Set trigger
                Collider col = destinationGuide.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                // Make transparent green
                Renderer rend = destinationGuide.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
                    if (mat.shader == null)
                    {
                        mat = new Material(Shader.Find("Standard"));
                        mat.SetFloat("_Mode", 3f); // Transparent mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                    }
                    mat.color = new Color(0f, 1f, 0f, 0.4f);
                    rend.material = mat;
                }
            }
        }
        else if (currentState == 3)
        {
            text1.text = "1. 택시에 타라";
            text1.color = Color.green;
            text2.text = "2. 승객을 찾아라";
            text2.color = Color.green;
            text3.text = "3. 목적지로 이동해라";
            text3.color = Color.green;
            text4.text = "4. 주차해라";
            text4.color = Color.white;

            // Destroy destination guide
            if (destinationGuide != null)
            {
                Destroy(destinationGuide);
                destinationGuide = null;
            }

            // Spawn Exit Passenger NPC once
            if (Application.isPlaying && GameObject.Find("ExitPassengerNPC") == null)
            {
                ThirdPersonCharacterController player = FindObjectOfType<ThirdPersonCharacterController>();
                if (player != null && player.transform.parent != null)
                {
                    SpawnExitPassenger(player.transform.parent);
                }
            }
        }
        else if (currentState == 4)
        {
            text1.text = "1. 택시에 타라";
            text1.color = Color.green;
            text2.text = "2. 승객을 찾아라";
            text2.color = Color.green;
            text3.text = "3. 목적지로 이동해라";
            text3.color = Color.green;
            text4.text = "4. 주차해라";
            text4.color = Color.green;

            StartCoroutine(SuccessSequence());
        }

        SetStrikethrough(text1, currentState >= 1, currentState == 1);
        SetStrikethrough(text2, currentState >= 2, currentState == 2);
        SetStrikethrough(text3, currentState >= 3, currentState == 3);
        SetStrikethrough(text4, currentState >= 4, currentState == 4);
    }

    private void SpawnExitPassenger(Transform taxi)
    {
        string prefabPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/Prefabs/Human_BasicMotionsDummy_M.prefab";
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#endif
        }

        if (prefab == null) return;

        // Spawn to the side of the taxi, forcing it to be on the Z-positive side
        Vector3 spawnPos = taxi.position;
        Vector3 sideDir = taxi.right;
        if (sideDir.z < 0)
        {
            sideDir = -sideDir; // Use the left side if the right side points in the Z-negative direction
        }
        
        spawnPos += sideDir * 1.8f;
        spawnPos.y = 0.5f;
        spawnPos.z += 1.0f; // Add Z+1 offset

        GameObject npc = Instantiate(prefab, spawnPos, Quaternion.LookRotation(Vector3.forward));
        npc.name = "ExitPassengerNPC";
        npc.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

        // Set Animator base controller first (it will be overridden inside ExitPassengerAI)
        Animator animator = npc.GetComponent<Animator>();
        string controllerPath = "Assets/PlayerAnimator.controller";
        RuntimeAnimatorController baseController = null;
#if UNITY_EDITOR
        baseController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
#endif
        if (baseController != null)
        {
            animator.runtimeAnimatorController = baseController;
        }

        // Add ExitPassengerAI
        npc.AddComponent<ExitPassengerAI>();
    }

    private IEnumerator SuccessSequence()
    {
        // Wait 1.8 seconds so the player can see the 4th mission strikethrough animation complete (1.6s)
        yield return new WaitForSeconds(1.8f);

        // Clear left side mission texts
        if (missionHeader != null) missionHeader.text = "";
        text1.text = "";
        text2.text = "";
        text3.text = "";
        text4.text = "";
        ResetAllStrikethroughs();

        yield return new WaitForSeconds(0.5f);

        // Show the SUCCESS! and reward popup in the center
        if (successPopupContainer != null)
        {
            successPopupContainer.SetActive(true);
            StartCoroutine(AnimateSuccessStrikethroughs());
        }

        yield return new WaitForSeconds(4.0f);

        // Hide the popup and reset success strikethroughs
        if (successPopupContainer != null)
        {
            successPopupContainer.SetActive(false);
            ResetSuccessStrikethroughs();
        }
    }

    public void SetState(int state)
    {
        if (state > currentState)
        {
            currentState = state;
            UpdateUI();
        }
    }
}
