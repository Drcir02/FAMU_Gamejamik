using UnityEngine;

public class PCComponent : MonoBehaviour
{
    [SerializeField] private PCTemperature pcTemperature;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(pcTemperature == null)
            pcTemperature = GetComponentInParent<PCTemperature>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Add enemy to the set when they enter the trigger
        if (other.CompareTag(pcTemperature.enemyTag))
        {
            pcTemperature.AddNearbyEnemy(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Remove enemy from the set when they leave the trigger
        if (other.CompareTag(pcTemperature.enemyTag))
        {
            pcTemperature.RemoveNearbyEnemy(other);
        }
    }
}
