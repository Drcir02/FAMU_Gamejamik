using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSLimiter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PC Temperature script. Auto-finds if left empty.")]
    public PCTemperature pcTemperature;
    [Tooltip("The main camera rendering the 3D game. Auto-finds if left empty.")]
    public Camera mainCamera;

    [Header("FPS Settings")]
    [Tooltip("The curve representing visual FPS. X-Axis (0 to 1) is Temperature. Y-Axis is Target FPS.")]
    public AnimationCurve fpsCurve;

    [Header("UI (Optional)")]
    [Tooltip("Assign a TextMeshPro UI element to display the current fps.")]
    public TMP_Text fpsText;

    // Internal rendering variables
    private RenderTexture activeRT;
    private RenderTexture frozenRT;
    private GameObject canvasObj;
    private RawImage displayImage;

    private float timer = 0f;
    private int currentTargetFPS = 120;

    private void Awake()
    {
        if (fpsCurve == null || fpsCurve.length == 0)
        {
            fpsCurve = new AnimationCurve(
                new Keyframe(0f, 120f, 0f, -400f),
                new Keyframe(1f, 1f, 0f, 0f)
            );
        }
    }

    private void Start()
    {
        if (pcTemperature == null)
            pcTemperature = GetComponent<PCTemperature>();
            
        if (mainCamera == null)
            mainCamera = Camera.main;

        // CRITICAL FIX: Force the underlying game engine to always run fast.
        // This ensures physics, jumping, and input never slow down or go into "slow motion".
        Application.targetFrameRate = 120;

        CreateRenderTextures();
        CreateStutterCanvas();
    }

    private void CreateRenderTextures()
    {
        activeRT = new RenderTexture(Screen.width, Screen.height, 24);
        frozenRT = new RenderTexture(Screen.width, Screen.height, 24);
        
        // Tell the camera to render to our texture instead of directly to the screen
        mainCamera.targetTexture = activeRT;
    }

    private void CreateStutterCanvas()
    {
        canvasObj = new GameObject("StutterCanvas_System");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Render behind standard UI elements (-9999). 
        // This means the UI (like temperature) will stay smooth at 120fps!
        canvas.sortingOrder = -9999; 

        GameObject imageObj = new GameObject("StutterDisplay");
        imageObj.transform.SetParent(canvasObj.transform, false);
        displayImage = imageObj.AddComponent<RawImage>();
        displayImage.texture = frozenRT;

        // Stretch RawImage to fill screen perfectly
        RectTransform rtRect = displayImage.rectTransform;
        rtRect.anchorMin = Vector2.zero;
        rtRect.anchorMax = Vector2.one;
        rtRect.offsetMin = Vector2.zero;
        rtRect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (pcTemperature == null || mainCamera == null) return;

        // Handle screen/window resizing safely
        if (Screen.width != activeRT.width || Screen.height != activeRT.height)
        {
            mainCamera.targetTexture = null;
            activeRT.Release();
            frozenRT.Release();
            CreateRenderTextures();
            displayImage.texture = frozenRT;
            Graphics.Blit(activeRT, frozenRT); // Force an immediate refresh
        }

        // 1. Calculate Target FPS
        float currentTemp = pcTemperature.CurrentTemperature;
        float baseTemp = pcTemperature.baseTemperature;
        float maxTemp = pcTemperature.maxTemperature;

        if (Mathf.Approximately(maxTemp, baseTemp)) return;

        float normalizedTemp = Mathf.Clamp01((currentTemp - baseTemp) / (maxTemp - baseTemp));
        float evaluatedFPS = fpsCurve.Evaluate(normalizedTemp);
        currentTargetFPS = Mathf.Max(1, Mathf.RoundToInt(evaluatedFPS));

        // 2. Visual Stutter Logic
        timer += Time.unscaledDeltaTime;
        float frameInterval = 1f / currentTargetFPS;

        if(currentTargetFPS <= 1f)
        {
            // This closes the game if it's a standalone build (or freezes WebGL)
            Application.Quit();
        }

        // If it's time for a new frame, capture it from the active camera to our frozen display
        if (timer >= frameInterval)
        {
            // We use modulo so we don't drift and lose time over many frames
            timer = timer % frameInterval;
            Graphics.Blit(activeRT, frozenRT);
        }

        // Update the UI if assigned
        if (fpsText != null)
        {
            // Display temperature as an integer
            fpsText.text = $"FPS: {Mathf.RoundToInt(currentTargetFPS)}";

            // Optional: change color when overheating
            fpsText.color = currentTargetFPS <= 16 ? Color.red : Color.white;
        }
    }

    private void OnDestroy()
    {
        // Cleanup memory properly so WebGL doesn't crash on scene reloads
        if (mainCamera != null) mainCamera.targetTexture = null;
        if (activeRT != null) activeRT.Release();
        if (frozenRT != null) frozenRT.Release();
        if (canvasObj != null) Destroy(canvasObj);
    }
}
