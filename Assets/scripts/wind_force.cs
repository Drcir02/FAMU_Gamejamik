using UnityEngine;

public class wind_force : MonoBehaviour
{
    [Header("Nastaveni vetru")]
    [Tooltip("Maximalni sila vetru tesne u vetraku.")]
    public float maxWindForce = 25f;

    [Tooltip("Smer, kterym vitr fouka (ve world space nebo local space).")]
    public Vector3 windDirection = Vector3.up;

    private Collider fanCollider;

    void Start()
    {
        fanCollider = GetComponent<Collider>();
    }

    void OnTriggerStay(Collider other)
    {
        // Zkontrolujeme, zda ma objekt, ktery vstoupil do vetru, Rigidbody
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Vypocteme linearni pokles sily podle vzdalenosti
            float forceMagnitude = CalculateWindForce(other.transform.position);

            // Aplikujeme silu smerem nahoru (nebo ve zvolenem smeru)
            // Pouzivame ForceMode.Force, coz bere v potaz hmotnost (mass) objektu
            rb.AddForce(windDirection.normalized * forceMagnitude, ForceMode.Force);
        }

        // Zkontrolujeme, zda vitr fouka na PC a ochlazujeme ho
        PCTemperature pc = other.GetComponent<PCTemperature>();
        if (pc != null)
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);
            float maxDistance = fanCollider.bounds.extents.y * 2f; 
            
            // 0 = nejvzdalenejsi, 1 = nejblize
            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
            float intensity = 1f - normalizedDistance;
            
            pc.ApplyFanCooling(intensity);
        }
    }

    float CalculateWindForce(Vector3 objectPosition)
    {
        // Vzdalenost mezi stredem zakladny vetraku (timto objektem) a objektem ve vzduchu
        float distance = Vector3.Distance(transform.position, objectPosition);

        // Zjistime maximalni dosah collideru (predpokladame BoxCollider na vysku po ose Y)
        float maxDistance = fanCollider.bounds.extents.y * 2f;

        // Normovana vzdalenost (0 = u vetraku, 1 = na konci dosahu)
        float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

        // Sila klesa s linearnim ubytkem (cim dal, tim slabsi)
        // Pro realistictejsi efekt muzes pouzit: Mathf.Pow(1 - normalizedDistance, 2)
        float currentForce = Mathf.Lerp(maxWindForce, 0f, normalizedDistance);

        return currentForce;
    }
}
