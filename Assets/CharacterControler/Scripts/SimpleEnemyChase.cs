using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class SimpleEnemyChase : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform this enemy chases.")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [Tooltip("Chase speed.")]
    [SerializeField] private float speed = 5f;

    [Tooltip("How fast the enemy accelerates toward target speed.")]
    [SerializeField] private float acceleration = 30f;

    [Tooltip("How close before the enemy stops moving.")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("World Detection")]
    [Tooltip("Layer mask for ground AND obstacles (can be the same layer).")]
    [SerializeField] private LayerMask worldLayer;

    [Header("Obstacle Avoidance")]
    [Tooltip("How far ahead the enemy looks for obstacles.")]
    [SerializeField] private float avoidanceRange = 2f;

    [Header("Fan Push Recovery")]
    [Tooltip("Seconds after last push before the enemy resumes chasing.")]
    [SerializeField] private float recoveryDelay = 0.4f;

    [Header("Jump Settings")]
    [Tooltip("The speed below which the enemy will attempt to jump.")]
    [SerializeField] private float speedThreshold = 1.5f;

    [Tooltip("The force applied upward when the enemy jumps.")]
    [SerializeField] private float jumpForce = 5.0f;

    [Tooltip("How long the enemy must wait between jumps (in seconds).")]
    [SerializeField] private float jumpCooldown = 2.0f;

    private float cooldownTimer = 0f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private float lastPushedTime = -10f;
    private bool hasLandedAfterPush = true;
    private Vector3 lastRecordedPos;
    private float stuckTimer = 0f;
    private Vector3 stuckEscapeDir = Vector3.zero;
    private float stuckEscapeTimer = 0f;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        // Auto-assign to Enemy layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            gameObject.layer = enemyLayer;
            foreach (Transform child in transform)
                child.gameObject.layer = enemyLayer;

            // Enemies don't physically push the player (layer 2 = Ignore Raycast)
            Physics.IgnoreLayerCollision(enemyLayer, 2, true);
            // Enemies don't physically push each other
            Physics.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        }

        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        lastRecordedPos = transform.position;
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        bool grounded = IsGrounded();
        if (grounded)
        {
            hasLandedAfterPush = true;
        }

        bool recentlyPushed = (Time.time - lastPushedTime) < recoveryDelay;

        if (!recentlyPushed && hasLandedAfterPush)
        {
            ChaseTarget();
            DetectStuck();
        }
        else
        {
            stuckTimer = 0f;
            stuckEscapeTimer = 0f;
        }
    }

    private void Update()
    {
        // Count down the cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // 1. Calculate the current horizontal speed (ignoring vertical falling/jumping velocity)
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // 2. Check if the enemy is moving too slow, and ensure it's grounded so it doesn't double-jump
        if (currentSpeed < speedThreshold && IsGrounded() && cooldownTimer <= 0)
        {
            Jump();
        }
    }

    void Jump()
    {
        // Apply an upward force (ForceMode.Impulse is ideal for instant actions like jumping)
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //rb.AddForce(Vector3.forward * (jumpForce/2), ForceMode.Impulse); // Optional: add a small forward push

        // Set the cooldown timer immediately
        cooldownTimer = jumpCooldown;
    }

    private void ChaseTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget < stoppingDistance)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.MoveTowards(vel.x, 0f, acceleration * Time.fixedDeltaTime);
            vel.z = Mathf.MoveTowards(vel.z, 0f, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = vel;
            return;
        }

        Vector3 desiredDir = toTarget.normalized;

        // Apply obstacle avoidance
        desiredDir = ApplyAvoidance(desiredDir);

        // If stuck, override with escape direction
        if (stuckEscapeTimer > 0f)
        {
            desiredDir = stuckEscapeDir;
            stuckEscapeTimer -= Time.fixedDeltaTime;
        }

        // Smoothly accelerate toward desired velocity
        Vector3 targetVel = desiredDir * speed;
        Vector3 currentVel = rb.linearVelocity;
        float newX = Mathf.MoveTowards(currentVel.x, targetVel.x, acceleration * Time.fixedDeltaTime);
        float newZ = Mathf.MoveTowards(currentVel.z, targetVel.z, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newX, currentVel.y, newZ);

        // Face movement direction
        if (desiredDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Avoidance rays are cast from chest height so they detect walls but not the floor.
    /// </summary>
    private Vector3 ApplyAvoidance(Vector3 desiredDir)
    {
        // Ray origin at chest height — high enough to clear the ground plane
        float chestHeight = capsule.height * 0.5f;
        Vector3 origin = transform.position + Vector3.up * chestHeight;

        bool forwardBlocked = Physics.Raycast(origin, desiredDir, avoidanceRange, worldLayer);

        if (!forwardBlocked)
            return desiredDir;

        Vector3 rightDir = Quaternion.Euler(0, 40, 0) * desiredDir;
        Vector3 leftDir = Quaternion.Euler(0, -40, 0) * desiredDir;

        bool rightBlocked = Physics.Raycast(origin, rightDir, avoidanceRange * 0.8f, worldLayer);
        bool leftBlocked = Physics.Raycast(origin, leftDir, avoidanceRange * 0.8f, worldLayer);

        if (!leftBlocked && !rightBlocked)
        {
            Vector3 cross = Vector3.Cross(Vector3.up, desiredDir);
            Vector3 toTarget = target.position - transform.position;
            return Vector3.Dot(toTarget, cross) > 0 ? rightDir.normalized : leftDir.normalized;
        }

        if (!leftBlocked) return leftDir.normalized;
        if (!rightBlocked) return rightDir.normalized;

        // Try wider angles
        Vector3 wideRight = Quaternion.Euler(0, 90, 0) * desiredDir;
        Vector3 wideLeft = Quaternion.Euler(0, -90, 0) * desiredDir;

        if (!Physics.Raycast(origin, wideLeft, avoidanceRange * 0.6f, worldLayer))
            return wideLeft.normalized;
        if (!Physics.Raycast(origin, wideRight, avoidanceRange * 0.6f, worldLayer))
            return wideRight.normalized;

        return -desiredDir;
    }

    private void DetectStuck()
    {
        stuckTimer += Time.fixedDeltaTime;
        if (stuckTimer >= 1f)
        {
            float movedDist = (transform.position - lastRecordedPos).sqrMagnitude;
            if (movedDist < 0.25f)
            {
                Vector3 toTarget = (target.position - transform.position).normalized;
                float randomSign = Random.value > 0.5f ? 1f : -1f;
                stuckEscapeDir = (Quaternion.Euler(0, 90 * randomSign, 0) * toTarget).normalized;
                stuckEscapeTimer = 0.8f;
            }
            lastRecordedPos = transform.position;
            stuckTimer = 0f;
        }
    }

    /// <summary>
    /// Ground check: ray from the bottom of the capsule, cast a short distance down.
    /// </summary>
    private bool IsGrounded()
    {
        // Bottom of capsule = transform.position + capsule.center - half height
        float bottomY = capsule.center.y - capsule.height * 0.5f;
        Vector3 feetPos = transform.position + new Vector3(0f, bottomY + 0.1f, 0f);
        return Physics.Raycast(feetPos, Vector3.down, 0.2f, worldLayer);
    }

    /// <summary>
    /// Called by the fan to push this enemy. Can be called every frame.
    /// </summary>
    public void ApplyFanForce(Vector3 force, bool restrictChase)
    {
        lastPushedTime = Time.time;
        if(restrictChase)
            hasLandedAfterPush = false;
        rb.AddForce(force, ForceMode.Force);
    }

    /// <summary>
    /// Called by the fan for a single instantaneous strong wave push.
    /// </summary>
    public void ApplyFanImpulse(Vector3 impulse, bool restrictChase)
    {
        lastPushedTime = Time.time;
        if(restrictChase)
            hasLandedAfterPush = false;
        rb.AddForce(impulse, ForceMode.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 toTarget = (target.position - transform.position);
        toTarget.y = 0;
        if (toTarget.sqrMagnitude < 0.01f) return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up, toTarget.normalized * 2f);

        // Avoidance rays
        float chestH = capsule != null ? capsule.height * 0.5f : 1f;
        Vector3 origin = transform.position + Vector3.up * chestH;
        Vector3 dir = toTarget.normalized;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, dir * avoidanceRange);
        Gizmos.DrawRay(origin, Quaternion.Euler(0, 40, 0) * dir * avoidanceRange * 0.8f);
        Gizmos.DrawRay(origin, Quaternion.Euler(0, -40, 0) * dir * avoidanceRange * 0.8f);

        // Ground check ray
        if (capsule != null)
        {
            float bottomY = capsule.center.y - capsule.height * 0.5f;
            Vector3 feetPos = transform.position + new Vector3(0f, bottomY + 0.1f, 0f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(feetPos, Vector3.down * 0.2f);
        }
    }
}
