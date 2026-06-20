using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PC Temperature script. Will auto-find on this object if left empty.")]
    public PCTemperature pcTemperature;

    [Header("FPS Settings")]
    [Tooltip("The curve representing FPS. X-Axis (0 to 1) is Temperature (Base to Max). Y-Axis is Target FPS. Edit the curve to change the exponential drop!")]
    public AnimationCurve fpsCurve;

    // Keep track of the current target frame rate so we don't set it every frame
    private int currentTargetFPS = -1;

    private void Awake()
    {
        // Setup a default exponential decay curve (drops fast, then levels out near 1)
        // You can edit this freely in the Unity Inspector!
        if (fpsCurve == null || fpsCurve.length == 0)
        {
            fpsCurve = new AnimationCurve(
                new Keyframe(0f, 120f, 0f, -400f), // At 0% heat (baseTemp), 120 FPS
                new Keyframe(1f, 1f, 0f, 0f)       // At 100% heat (maxTemp), 1 FPS
            );
        }
    }

    private void Start()
    {
        if (pcTemperature == null)
        {
            pcTemperature = GetComponent<PCTemperature>();
        }
    }

    private void Update()
    {
        if (pcTemperature == null) return;

        // Calculate how far we are between base and max temperature (0.0 to 1.0)
        float currentTemp = pcTemperature.CurrentTemperature;
        float baseTemp = pcTemperature.baseTemperature;
        float maxTemp = pcTemperature.maxTemperature;

        // Prevent division by zero if someone accidentally sets base == max
        if (Mathf.Approximately(maxTemp, baseTemp)) return;

        // 0.0 means we are at or below base temperature. 1.0 means we are at max temperature.
        float normalizedTemp = Mathf.Clamp01((currentTemp - baseTemp) / (maxTemp - baseTemp));

        // Evaluate the curve to get the desired FPS
        float evaluatedFPS = fpsCurve.Evaluate(normalizedTemp);

        // Round to nearest integer and ensure it doesn't drop below 1
        int targetFPS = Mathf.Max(1, Mathf.RoundToInt(evaluatedFPS));

        // Only update Application.targetFrameRate if it has actually changed
        // This prevents unnecessary overhead or micro-stutters
        if (targetFPS != currentTargetFPS)
        {
            currentTargetFPS = targetFPS;
            Application.targetFrameRate = currentTargetFPS;
        }
    }
}
