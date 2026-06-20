using UnityEngine;

public class wind_force : MonoBehaviour
{
    [Header("Nastavení vìtru")]
    [Tooltip("Maximální síla vìtru tìsnì u vìtráku.")]
    public float maxWindForce = 25f;

    [Tooltip("Smìr, kterým vítr fouká (ve world space nebo local space).")]
    public Vector3 windDirection = Vector3.up;

    private Collider fanCollider;

    void Start()
    {
        fanCollider = GetComponent<Collider>();
    }

    void OnTriggerStay(Collider other)
    {
        // Zkontrolujeme, zda má objekt, který vstoupil do vìtru, Rigidbody
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Vypoèítáme lineární pokles síly podle vzdálenosti
            float forceMagnitude = CalculateWindForce(other.transform.position);

            // Aplikujeme sílu smìrem nahoru (nebo ve zvoleném smìru)
            // Používáme ForceMode.Force, což bere v potaz hmotnost (mass) objektu
            rb.AddForce(windDirection.normalized * forceMagnitude, ForceMode.Force);
        }
    }

    float CalculateWindForce(Vector3 objectPosition)
    {
        // Vzdálenost mezi støedem základny vìtráku (tímto objektem) a objektem ve vzduchu
        float distance = Vector3.Distance(transform.position, objectPosition);

        // Zjistíme maximální dosah collideru (pøedpokládáme BoxCollider na výšku po ose Y)
        float maxDistance = fanCollider.bounds.extents.y * 2f;

        // Normovaná vzdálenost (0 = u vìtráku, 1 = na konci dosahu)
        float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

        // Síla klesá s lineárním úbytkem (èím dál, tím slabší)
        // Pro realistiètìjší efekt mùžeš použít: Mathf.Pow(1 - normalizedDistance, 2)
        float currentForce = Mathf.Lerp(maxWindForce, 0f, normalizedDistance);

        return currentForce;
    }
}