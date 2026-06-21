using UnityEngine;

public class Billboard : MonoBehaviour
{
    public enum BillboardType
    {
        Horizontal, // rotate only around Y (yaw) - good for ground objects/characters
        Vertical,   // rotate only around X (pitch)
        Classic     // full 3D look-at
    }

    [SerializeField, Tooltip("Target to face. If empty, Camera.main will be used (if available).")]
    private Transform target;

    [SerializeField, Tooltip("Choose billboard behaviour.")]
    private BillboardType type = BillboardType.Horizontal;

    [SerializeField, Tooltip("Smoothing speed. Higher = faster rotation.")]
    [Range(0.01f, 50f)]
    private float smoothSpeed = 5f;

    void Reset()
    {
        // When component is first added, default to main camera if present
        if (Camera.main != null)
            target = Camera.main.transform;
    }

    void Update()
    {
        if (target == null)
        {
            if (Camera.main == null) return;
            target = Camera.main.transform;
        }

        Vector3 direction = target.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        // Base look rotation
        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
        Vector3 euler = lookRotation.eulerAngles;

        // Constrain rotation depending on chosen type
        switch (type)
        {
            case BillboardType.Horizontal:
                euler.x = 0f;
                euler.z = 0f;
                break;
            case BillboardType.Vertical:
                euler.y = 0f;
                euler.z = 0f;
                break;
            case BillboardType.Classic:
            default:
                break;
        }

        Quaternion targetRotation = Quaternion.Euler(euler);

        // Smooth interpolation (time-independent)
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }

    // Optional runtime helpers
    public void SetTarget(Transform newTarget) => target = newTarget;
    public Transform GetTarget() => target;
}
