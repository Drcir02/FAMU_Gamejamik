using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class PCTemperature : MonoBehaviour
{
    [Header("Temperature Settings")]
    [Tooltip("The starting and minimum temperature of the PC.")]
    public float baseTemperature = 30f;
    [Tooltip("The maximum temperature the PC can reach.")]
    public float maxTemperature = 100f;
    [Tooltip("Temperature at which the PC is considered to be overheating.")]
    public float overheatThreshold = 90f;

    [Header("Heating & Cooling")]
    [Tooltip("How much the temperature increases per second for EACH nearby enemy.")]
    public float heatIncreasePerEnemy = 5f;
    [Tooltip("How much the temperature decreases per second constantly.")]
    public float coolingRate = 2f;
    [Tooltip("Maximum extra cooling applied per second when the fan is blowing directly on the PC.")]
    public float maxFanCoolingBonus = 15f;

    [Header("Detection Settings")]
    [Tooltip("The tag used to identify enemies. Ensure your enemies have this tag.")]
    public string enemyTag = "Enemy";

    [Header("UI (Optional)")]
    [Tooltip("Assign a TextMeshPro UI element to display the current temperature.")]
    public TMP_Text temperatureText;

    // Public properties for other scripts or UI to access
    public float CurrentTemperature { get; private set; }
    public bool IsOverheating { get; private set; }

    // Keep track of enemies currently inside the trigger
    private HashSet<Collider> nearbyEnemies = new HashSet<Collider>();

    private void Start()
    {
        // Initialize temperature
        CurrentTemperature = baseTemperature;

        // Check if the collider is set to "Is Trigger"
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("PCTemperature script is attached to a GameObject without a Trigger Collider. Please check 'Is Trigger' on the Collider component.");
        }
    }

    private void Update()
    {
        // Clean up any destroyed or deactivated enemies from the set to avoid errors
        nearbyEnemies.RemoveWhere(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);

        int enemyCount = nearbyEnemies.Count;

        // Apply heat from enemies
        if (enemyCount > 0)
        {
            float heatAdded = enemyCount * heatIncreasePerEnemy;
            CurrentTemperature += heatAdded * Time.deltaTime;
        }

        // Apply constant cooling
        CurrentTemperature -= coolingRate * Time.deltaTime;

        // Clamp the temperature so it doesn't go below base or above max
        CurrentTemperature = Mathf.Clamp(CurrentTemperature, baseTemperature, maxTemperature);

        // Update overheating status flag
        IsOverheating = CurrentTemperature >= overheatThreshold;

        // Update the UI if assigned
        if (temperatureText != null)
        {
            // Display temperature as an integer
            temperatureText.text = $"Temp: {Mathf.RoundToInt(CurrentTemperature)}°C";

            // Optional: change color when overheating
            temperatureText.color = IsOverheating ? Color.red : Color.white;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Add enemy to the set when they enter the trigger
        if (other.CompareTag(enemyTag))
        {
            nearbyEnemies.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Remove enemy from the set when they leave the trigger
        if (other.CompareTag(enemyTag))
        {
            nearbyEnemies.Remove(other);
        }
    }

    public void ApplyFanCooling(float intensity)
    {
        // Intensity is a value between 0 (far away) and 1 (point blank)
        float additionalCooling = maxFanCoolingBonus * intensity * Time.deltaTime;
        CurrentTemperature -= additionalCooling;

        // Ensure we don't instantly drop below base temperature before Update() clamps it
        CurrentTemperature = Mathf.Max(CurrentTemperature, baseTemperature);
    }
}
