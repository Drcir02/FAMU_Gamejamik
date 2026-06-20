using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFan : MonoBehaviour
{
    [Header("Fan Settings")]
    [Tooltip("Which mouse button activates the fan: Left, Right, or Middle.")]
    [SerializeField] private MouseButton mouseButton = MouseButton.Left;

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

    [Header("Visuals")]
    [Tooltip("Optional ParticleSystem for the normal fan. Its shape and lifetime will automatically sync with the fan's range and angle.")]
    [SerializeField] private ParticleSystem normalFanParticles;

    [Tooltip("Optional ParticleSystem for the recoil blast. Its shape and lifetime will automatically sync with the recoil angle and range.")]
    [SerializeField] private ParticleSystem recoilBlastParticles;

    [Header("Propeller Rotation")]
    [Tooltip("The actual propeller model to rotate.")]
    [SerializeField] private Transform propellerTransform;

    [Tooltip("Normal spinning speed in degrees per second when blowing.")]
    [SerializeField] private float normalSpinSpeed = 1000f;

    [Tooltip("Additional burst of spin speed added instantly when recoil is used.")]
    [SerializeField] private float recoilSpinBurst = 3000f;

    [Tooltip("How fast the spin smoothly catches up to the target speed. Higher is faster.")]
    [SerializeField] private float spinAcceleration = 5f;

    private float currentSpinSpeed = 0f;

    [Tooltip("Reference to the player's CharacterControllerBase. If empty, finds it in parent.")]
    [SerializeField] private CharacterControllerBase playerController;

    [Header("Self Push (Recoil)")]
    [Tooltip("Which mouse button activates the self-push backward jump.")]
    [SerializeField] private MouseButton recoilMouseButton = MouseButton.Right;

    [Tooltip("Force applied to the player when using the recoil push.")]
    [SerializeField] private float playerPushForce = 15f;

    [Tooltip("Velocity impulse applied to the player backwards.")]
    [SerializeField] private float recoilVelocity = 15f;

    [Tooltip("Impulse force applied to enemies in front of the player.")]
    [SerializeField] private float recoilEnemyForce = 25f;

    [Tooltip("Half-angle of the blast cone for enemies (wider than normal fan).")]
    [SerializeField] private float recoilConeAngle = 60f;

    [Tooltip("Cooldown for the recoil push in seconds.")]
    [SerializeField] private float recoilCooldown = 2f;

    private float lastRecoilTime = -9999f;
    private bool wasBlowing = false;

    private enum MouseButton { Left, Right, Middle }

    // Pre-allocated buffer — no garbage collection
    private readonly Collider[] overlapBuffer = new Collider[32];

    private void OnValidate()
    {
        SyncParticleSystems();
    }

    private void Start()
    {
        if (aimTransform == null && Camera.main != null)
            aimTransform = Camera.main.transform;

        if (playerController == null)
            playerController = GetComponentInParent<CharacterControllerBase>();

        SyncParticleSystems();
    }

    private void SyncParticleSystems()
    {
        if (normalFanParticles != null)
        {
            var shape = normalFanParticles.shape;
            shape.angle = fanConeAngle * .8f;
            
            var main = normalFanParticles.main;
            // Calculate lifetime based on speed so it travels exactly 'fanRange'
            if (main.startSpeed.mode == ParticleSystemCurveMode.Constant && main.startSpeed.constant > 0.01f)
            {
                main.startLifetime = fanRange / main.startSpeed.constant;
            }
        }

        if (recoilBlastParticles != null)
        {
            var shape = recoilBlastParticles.shape;
            shape.angle = recoilConeAngle * .8f;
            
            var main = recoilBlastParticles.main;
            if (main.startSpeed.mode == ParticleSystemCurveMode.Constant && main.startSpeed.constant > 0.01f)
            {
                main.startLifetime = fanRange / main.startSpeed.constant;
            }
        }
    }

    private void Update()
    {
        if (aimTransform == null || Mouse.current == null) return;

        bool isBlowing = mouseButton switch
        {
            MouseButton.Left   => Mouse.current.leftButton.isPressed,
            MouseButton.Right  => Mouse.current.rightButton.isPressed,
            MouseButton.Middle => Mouse.current.middleButton.isPressed,
            _ => false
        };

        if (isBlowing)
        {
            BlowEnemiesInCone();
            
            if (!wasBlowing)
            {
                if (normalFanParticles != null) normalFanParticles.Play();
            }
        }
        else
        {
            if (wasBlowing)
            {
                if (normalFanParticles != null) normalFanParticles.Stop();
            }
        }

        wasBlowing = isBlowing;

        // --- Propeller Rotation Logic ---
        if (propellerTransform != null)
        {
            float targetSpinSpeed = isBlowing ? normalSpinSpeed : 0f;
            
            // Smoothly move the current spin speed towards the target speed
            currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, targetSpinSpeed, Time.deltaTime * spinAcceleration);
            
            // Apply rotation around local X axis
            propellerTransform.Rotate(Vector3.forward * currentSpinSpeed * Time.deltaTime, Space.Self);
        }

        bool isRecoilPressed = recoilMouseButton switch
        {
            MouseButton.Left   => Mouse.current.leftButton.wasPressedThisFrame,
            MouseButton.Right  => Mouse.current.rightButton.wasPressedThisFrame,
            MouseButton.Middle => Mouse.current.middleButton.wasPressedThisFrame,
            _ => false
        };

        if (isRecoilPressed && Time.time >= lastRecoilTime + recoilCooldown)
        {
            ApplyRecoil();
        }
    }

    private void ApplyRecoil()
    {
        if (playerController == null) return;
        
        lastRecoilTime = Time.time;
        
        // Push player backward
        Vector3 pushDir = -aimTransform.forward;
        Vector3 finalForce = pushDir * playerPushForce * 50f;
        playerController.AddExternalVelocity(finalForce);

        // Play the blast particle effect
        if (recoilBlastParticles != null)
        {
            recoilBlastParticles.Play();
        }

        // Add a huge burst of spin speed
        currentSpinSpeed += recoilSpinBurst;

        // Blast enemies forward in a wide cone
        BlastEnemiesInCone();
    }

    private void BlastEnemiesInCone()
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

            if (Vector3.Angle(aimFlat, toEnemy.normalized) <= recoilConeAngle)
            {
                // Similar to normal blowing, but uses impulse and stronger upward bias for a dramatic blast
                Vector3 blastDir = (toEnemy.normalized + Vector3.up * upwardBias * 1.5f).normalized;
                enemy.ApplyFanImpulse(blastDir * recoilEnemyForce, true);
            }
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
                enemy.ApplyFanForce(pushDir * fanForce, true);
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
            
            // --- Normal Fan Cone (Cyan) ---
            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
            DrawWireCone(transform.position, forward, fanConeAngle, fanRange, 24);
            
            // --- Recoil Blast Cone (Magenta) ---
            Gizmos.color = new Color(1f, 0f, 1f, 0.4f);
            DrawWireCone(transform.position, forward, recoilConeAngle, fanRange, 24);
        }
    }

    /// <summary>
    /// Helper to draw a true 3D wireframe cone using Gizmos.
    /// </summary>
    private void DrawWireCone(Vector3 position, Vector3 direction, float angle, float length, int segments)
    {
        Vector3 forward = direction.normalized;
        
        // Find a perpendicular up vector for the base
        Vector3 right = Vector3.Cross(forward, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(forward, Vector3.right);
        right.Normalize();
        Vector3 up = Vector3.Cross(right, forward).normalized;

        Vector3[] edgePoints = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float rotAngle = i * 360f / segments;
            Quaternion rot = Quaternion.AngleAxis(rotAngle, forward);
            
            // 1. Get a vector that is 'angle' degrees away from 'forward'
            Vector3 outerDir = Quaternion.AngleAxis(angle, up) * forward;
            
            // 2. Rotate it around 'forward' to form the cone
            Vector3 finalDir = rot * outerDir;
            
            edgePoints[i] = position + finalDir * length;
            
            // Draw line from tip to edge (only draw some of them to prevent clutter)
            if (i % 4 == 0)
            {
                Gizmos.DrawLine(position, edgePoints[i]);
            }
        }
        
        // Draw the base polygon (the circle at the end of the cone)
        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(edgePoints[i], edgePoints[(i + 1) % segments]);
        }
    }
}
