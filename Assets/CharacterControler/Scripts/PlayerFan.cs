using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFan : MonoBehaviour
{
    [Header("Fan Settings")]
    [Tooltip("Force applied to enemies each frame while the fan is active.")]
    [SerializeField] private float fanForce = 40f;

    [Tooltip("How far the fan can reach.")]
    [SerializeField] private float fanRange = 12f;

    [Tooltip("Half-angle of the fan's cone in degrees.")]
    [SerializeField] private float fanConeAngle = 30f;

    [Tooltip("Upward bias when pushing enemies (0 = purely horizontal, 1 = strong upward arc).")]
    [SerializeField] private float upwardBias = 0.5f;

    [Header("References")]
    [Tooltip("The camera or transform whose forward direction defines where the fan points. If left empty, uses Camera.main.")]
    [SerializeField] private Transform aimTransform;

    [Header("Input")]
    [Tooltip("Which mouse button activates the fan: Left, Right, or Middle.")]
    [SerializeField] private MouseButton mouseButton = MouseButton.Left;

    private enum MouseButton { Left, Right, Middle }

    // Pre-allocated buffer — no garbage collection
    private readonly Collider[] overlapBuffer = new Collider[32];

    private void Start()
    {
        if (aimTransform == null && Camera.main != null)
            aimTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (aimTransform == null || Mouse.current == null) return;

        bool isPressed = mouseButton switch
        {
            MouseButton.Left   => Mouse.current.leftButton.isPressed,
            MouseButton.Right  => Mouse.current.rightButton.isPressed,
            MouseButton.Middle => Mouse.current.middleButton.isPressed,
            _ => false
        };

        if (isPressed)
        {
            BlowEnemiesInCone();
        }
    }

    private void BlowEnemiesInCone()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, fanRange, overlapBuffer);

        Vector3 aimFlat = aimTransform.forward;
        aimFlat.y = 0f;
        if (aimFlat.sqrMagnitude < 0.001f) return;
        aimFlat.Normalize();

        for (int i = 0; i < hitCount; i++)
        {
            SimpleEnemyChase enemy = overlapBuffer[i].GetComponent<SimpleEnemyChase>();
            if (enemy == null) continue;

            Vector3 toEnemy = overlapBuffer[i].transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude < 0.01f) continue;

            if (Vector3.Angle(aimFlat, toEnemy.normalized) <= fanConeAngle)
            {
                Vector3 pushDir = (toEnemy.normalized + Vector3.up * upwardBias).normalized;
                enemy.ApplyFanForce(pushDir * fanForce);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, fanRange);

        if (aimTransform != null)
        {
            Vector3 forward = aimTransform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, forward * fanRange);

            Vector3 leftEdge = Quaternion.Euler(0, -fanConeAngle, 0) * forward;
            Vector3 rightEdge = Quaternion.Euler(0, fanConeAngle, 0) * forward;
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawRay(transform.position, leftEdge * fanRange);
            Gizmos.DrawRay(transform.position, rightEdge * fanRange);
        }
    }
}
