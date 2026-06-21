using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Targets")]
    [Tooltip("The enemy prefab (capsule with SimpleEnemyChase component).")]
    [SerializeField] private GameObject enemyPrefab;
    
    [Tooltip("The target that spawned enemies should chase.")]
    [SerializeField] private Transform chaseTarget;

    [Header("Spawn Positioning")]
    [Tooltip("Radius around the spawner within which enemies will spawn.")]
    [SerializeField] private float spawnRadius = 20f;

    [Tooltip("Layer mask representing the ground to raycast against.")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Height above the spawner position to start the ground raycast.")]
    [SerializeField] private float raycastStartHeight = 20f;

    [Tooltip("Max distance to cast the ray downwards to find the ground.")]
    [SerializeField] private float raycastDistance = 40f;

    [Tooltip("Height offset above the ground to spawn the enemy so it doesn't clip.")]
    [SerializeField] private float spawnHeightOffset = 1f;

    [Header("Wave Settings")]
    [Tooltip("Total number of waves to spawn. Set to 0 or negative for infinite waves.")]
    [SerializeField] private int totalWaves = 5;

    [Tooltip("Number of enemies to spawn in each wave.")]
    [SerializeField] private int enemiesPerWave = 5;

    [Tooltip("Multiplier to increase the number of enemies in each subsequent wave.")]
    [SerializeField] private float enemiesPerWaveMultiplier = 2f;

    [Tooltip("Delay in seconds between waves.")]
    [SerializeField] private float timeBetweenWaves = 10f;

    [Tooltip("Delay in seconds between individual enemy spawns within a wave.")]
    [SerializeField] private float timeBetweenSpawns = 0.5f;

    private int currentWaveIndex = 0;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemyPrefab is not assigned in the EnemySpawner inspector!", this);
            return;
        }

        StartCoroutine(WaveControllerCoroutine());
    }

    private IEnumerator WaveControllerCoroutine()
    {
        while (totalWaves <= 0 || currentWaveIndex < totalWaves)
        {
            currentWaveIndex++;
            Debug.Log($"[Spawner] Starting Wave {currentWaveIndex}");

            yield return StartCoroutine(SpawnWaveCoroutine());

            Debug.Log($"[Spawner] Wave {currentWaveIndex} completed. Next wave in {timeBetweenWaves}s.");
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("[Spawner] All waves completed!");
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        int spawnedCount = 0;

        while (spawnedCount < enemiesPerWave + (currentWaveIndex * enemiesPerWaveMultiplier))
        {
            if (TryGetRandomSpawnPosition(out Vector3 spawnPosition))
            {
                SpawnEnemy(spawnPosition);
                spawnedCount++;
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
            else
            {
                yield return null;
            }
        }
    }

    private bool TryGetRandomSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 rayStart = new Vector3(
            transform.position.x + randomOffset.x,
            transform.position.y + raycastStartHeight,
            transform.position.z + randomOffset.y
        );

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            position = hit.point + Vector3.up * spawnHeightOffset;
            return true;
        }

        return false;
    }

    private void SpawnEnemy(Vector3 spawnPosition)
    {
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        if (enemyInstance.TryGetComponent<SimpleEnemyChase>(out var chaseScript))
        {
            if(chaseTarget != null)
                chaseScript.Target = chaseTarget;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
