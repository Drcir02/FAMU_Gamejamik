using System.Collections.Generic;
using UnityEngine;

public class LagSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PC Temperature script.")]
    public PCTemperature pcTemperature;
    [Tooltip("The prefab to spawn when it gets hot.")]
    public GameObject lagPrefab;
    [Tooltip("The parent object where lag objects will be spawned (also determines spawn position).")]
    public Transform lagsParent;

    [Header("Scaling Settings")]
    [Tooltip("Temperature at which objects start spawning.")]
    public float temperatureThreshold = 35f;
    [Tooltip("Multiplier for the number of objects.")]
    public float growthMultiplier = 0.5f;
    [Tooltip("The exponent for scaling (e.g., 2 is quadratic, 3 is cubic). Higher means it grows much faster.")]
    public float exponent = 2.0f;

    // List to keep track of spawned objects
    private List<GameObject> activeLagObjects = new List<GameObject>();

    private void Update()
    {
        // Make sure all references are assigned before doing anything
        if (pcTemperature == null || lagPrefab == null || lagsParent == null)
        {
            return;
        }

        // Clean up any nulls in case objects were destroyed manually or by other scripts
        activeLagObjects.RemoveAll(obj => obj == null);

        // Calculate how many objects we SHOULD have based on current temperature
        int targetCount = CalculateTargetCount(pcTemperature.CurrentTemperature);

        // Spawn objects if we have fewer than the target count
        while (activeLagObjects.Count < targetCount)
        {
            SpawnLagObject();
        }

        // Destroy objects if we have more than the target count (PC is cooling down)
        while (activeLagObjects.Count > targetCount && activeLagObjects.Count > 0)
        {
            DestroyLagObject();
        }
    }

    private int CalculateTargetCount(float currentTemp)
    {
        // If temperature is below threshold, we want 0 objects
        if (currentTemp <= temperatureThreshold)
        {
            return 0;
        }

        // Calculate how far above the threshold we are
        float tempDiff = currentTemp - temperatureThreshold;

        // Calculate target count: Multiplier * (TempDiff ^ Exponent)
        // This creates the exponential curve for the object count
        float calculatedFloat = growthMultiplier * Mathf.Pow(tempDiff, exponent);

        // Prevent overflow if the number gets ridiculously huge
        if (calculatedFloat > int.MaxValue)
        {
            return int.MaxValue;
        }

        return Mathf.FloorToInt(calculatedFloat);
    }

    private void SpawnLagObject()
    {
        // Spawn at the parent's position and rotation, childed to the parent
        GameObject newLag = Instantiate(lagPrefab, lagsParent.position, lagsParent.rotation, lagsParent);
        activeLagObjects.Add(newLag);
    }

    private void DestroyLagObject()
    {
        // Remove and destroy the most recently spawned object (LIFO)
        int lastIndex = activeLagObjects.Count - 1;
        GameObject objToDestroy = activeLagObjects[lastIndex];
        
        activeLagObjects.RemoveAt(lastIndex);
        Destroy(objToDestroy);
    }
}
