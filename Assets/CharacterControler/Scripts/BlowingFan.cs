using UnityEngine;
public class BlowingFan : MonoBehaviour
{
    private enum WindDirection { ForwardZ, UpY, RightX }
    [Header("Detection Settings")]
    [Tooltip("If true, the fan will use a Trigger Collider attached to this GameObject (or children) via OnTriggerStay. " +
             "If false, the fan will automatically detect objects in a cylindrical volume using a physics query.")]
    [SerializeField] private bool useTriggerCollider = false;
    [Tooltip("Which local axis of the fan GameObject defines the blowing direction.")]
    [SerializeField] private WindDirection windDirectionAxis = WindDirection.UpY;
    [Tooltip("Layer mask of objects that can be pushed by the fan.")]
    [SerializeField] private LayerMask affectLayers = ~0;
    [Header("Cylinder Dimensions (if not using trigger)")]
    [Tooltip("Radius of the wind cylinder.")]
    [SerializeField] private float windRadius = 2f;
    [Tooltip("Length of the wind cylinder.")]
    [SerializeField] private float windLength = 10f;
    [Header("Force Settings")]
    [Tooltip("Base force magnitude applied to the rigidbodies.")]
    [SerializeField] private float windForce = 20f;
    [Tooltip("Force mode used when applying the push.")]
    [SerializeField] private ForceMode forceMode = ForceMode.Force;
    [Tooltip("Upward bias (0 = straight along fan's forward direction, 1 = adds a strong upward arc to lift objects).")]
    [Range(0f, 1f)]
    [SerializeField] private float upwardBias = 0.2f;
    [Tooltip("If true, the force will gradually decay to 0 at the end of the range.")]
    [SerializeField] private bool decayWithDistance = true;
    [Header("Visual Debugging")]
    [Tooltip("Color of the cylinder gizmo in the editor.")]
    [SerializeField] private Color gizmoColor = new Color(0f, 0.75f, 1f, 0.3f);
    // Pre-allocated overlap buffer to avoid garbage collection
    private readonly Collider[] overlapBuffer = new Collider[64];
    private Vector3 GetWindDirection()
    {
        switch (windDirectionAxis)
        {
            case WindDirection.ForwardZ: return transform.forward;
            case WindDirection.UpY: return transform.up;
            case WindDirection.RightX: return transform.right;
            default: return transform.forward;
        }
    }
    private void FixedUpdate()
    {
        if (useTriggerCollider) return;
        BlowObjectsInCylinder();
    }
    private void OnTriggerStay(Collider other)
    {
        if (!useTriggerCollider) return;
        ApplyForceToCollider(other);
    }
    private void BlowObjectsInCylinder()
    {
        Vector3 fanPos = transform.position;
        Vector3 fanForward = GetWindDirection();
        // A capsule query encompasses the cylinder. 
        // We will perform the query and then mathematically filter to match the cylinder precisely.
        Vector3 point0 = fanPos;
        Vector3 point1 = fanPos + fanForward * windLength;
        int hitCount = Physics.OverlapCapsuleNonAlloc(point0, point1, windRadius, overlapBuffer, affectLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;
            // Mathematical cylinder check
            Vector3 targetPos = rb.worldCenterOfMass;
            Vector3 toTarget = targetPos - fanPos;
            float projection = Vector3.Dot(toTarget, fanForward);
            // Check if the object is within the length of the cylinder
            if (projection >= 0f && projection <= windLength)
            {
                // Check if the object is within the radius of the cylinder
                Vector3 perpendicular = toTarget - projection * fanForward;
                if (perpendicular.sqrMagnitude <= windRadius * windRadius)
                {
                    ApplyForceToRigidbody(rb, projection);
                }
            }
        }
    }
    private void ApplyForceToCollider(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;
        // Check layer mask
        if (((1 << other.gameObject.layer) & affectLayers) == 0) return;
        Vector3 targetPos = rb.worldCenterOfMass;
        Vector3 toTarget = targetPos - transform.position;
        float distance = Vector3.Dot(toTarget, GetWindDirection());
        ApplyForceToRigidbody(rb, distance);
    }
    private void ApplyForceToRigidbody(Rigidbody rb, float distance)
    {
        // Calculate decay
        float forceScale = 1f;
        if (decayWithDistance)
        {
            forceScale = Mathf.Clamp01(1f - (distance / windLength));
        }
        // Calculate direction and magnitude
        Vector3 pushDirection = (GetWindDirection() + Vector3.up * upwardBias).normalized;
        Vector3 finalForce = pushDirection * (windForce * forceScale);
        // Apply force based on component type to prevent script conflicts:
        // 1. SimpleEnemyChase has its own method which puts the enemy in a recovery state
        SimpleEnemyChase enemy = rb.GetComponent<SimpleEnemyChase>();
        if (enemy != null)
        {
            enemy.ApplyFanForce(finalForce, false);
            return;
        }
        // 2. CharacterControllerBase overrides linearVelocity manually.
        // We modify linearVelocity directly to bypass script execution order conflicts.
        if (rb.TryGetComponent<CharacterControllerBase>(out var playerCC))
        {
            Vector3 acceleration = finalForce;
            if (forceMode == ForceMode.Force)
            {
                acceleration = finalForce / rb.mass;
            }
            rb.linearVelocity += acceleration * Time.fixedDeltaTime;
            rb.AddForce(finalForce * 30f, ForceMode.Force);
        }
        else
        {
            // 3. General Rigidbody props (crates, barrels, etc.)
            rb.AddForce(finalForce, forceMode);
        }
    }
    private void OnDrawGizmos()
    {
        if (useTriggerCollider) return;
        Gizmos.color = gizmoColor;
        Vector3 start = transform.position;
        Vector3 direction = GetWindDirection();
        Vector3 end = transform.position + direction * windLength;
        DrawWireCylinder(start, end, windRadius);
        // Draw helper arrows to indicate wind direction
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 1.5f);
        int arrowCount = Mathf.Max(1, Mathf.FloorToInt(windLength / 2f));
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
        for (int i = 1; i <= arrowCount; i++)
        {
            float t = (float)i / (arrowCount + 1);
            Vector3 pos = Vector3.Lerp(start, end, t);
            Gizmos.DrawRay(pos - direction * 0.5f, direction * 1f);

            // Draw a tiny circle at each arrow position to simulate particles
            DrawCircle(pos, rotation, windRadius * 0.5f);
        }
    }
    private void DrawWireCylinder(Vector3 start, Vector3 end, float radius)
    {
        Vector3 direction = end - start;
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
        // Draw circles
        DrawCircle(start, rotation, radius);
        DrawCircle(end, rotation, radius);
        // Draw side lines
        Vector3 right = rotation * Vector3.right * radius;
        Vector3 up = rotation * Vector3.up * radius;
        Gizmos.DrawLine(start + right, end + right);
        Gizmos.DrawLine(start - right, end - right);
        Gizmos.DrawLine(start + up, end + up);
        Gizmos.DrawLine(start - up, end - up);
    }
    private void DrawCircle(Vector3 position, Quaternion rotation, float radius)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
        int segments = 16;
        Vector3 lastPoint = new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            Vector3 nextPoint = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
        Gizmos.matrix = oldMatrix;
    }
}