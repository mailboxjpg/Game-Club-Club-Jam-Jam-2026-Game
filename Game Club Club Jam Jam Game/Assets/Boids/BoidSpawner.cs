using System.Collections.Generic;
using UnityEngine;

public class BoidSpawner : MonoBehaviour {
    #region Fields
    
    [Header("Prefab")]
    [SerializeField] GameObject boidPrefab;
    
    [Header("Spawn")]
    [SerializeField, Min(1)] int boidCount = 60;
    [SerializeField] Vector2 spawnExtents = new(14f, 6f);
    [SerializeField, Min(0.1f)] float boidScale = 0.5f;
    
    [Header("Movement")]
    [SerializeField, Min(0.1f)] float baseSpeed = 7f;
    [SerializeField, Min(0f)] float speedVariance = 1.5f;
    [SerializeField] Vector2 flockBoundsSize = new(60f, 60f);
    
    [Header("Depth (Z) Isolation")]
    [Tooltip("All spawned boids are placed on this Z so they stay grouped as their own layer, separable from the rest of the scene, and never physically interact with anything outside it (2D physics is Z-agnostic, but a shared Z keeps them visually/spatially isolated).")]
    [SerializeField] float boidZ = -1f;
    
    readonly List<BoidAgent> spawnedBoids = new();
    Transform runtimeRoot;
    
    #endregion
    
    protected void Start() => SpawnFlock();

    [ContextMenu("Spawn Flock")]
    public void SpawnFlock() {
        ClearFlock();
        EnsureRuntimeRoot();

        for (var i = 0; i < boidCount; i++) {
            var boid = CreateBoid(i);
            spawnedBoids.Add(boid);
        }
    }

    [ContextMenu("Clear Flock")]
    public void ClearFlock() {
        for (var i = spawnedBoids.Count - 1; i >= 0; i--) {
            var boid = spawnedBoids[i];
            if (boid) DestroyImmediate(boid.gameObject);
        }
        
        spawnedBoids.Clear();
        
        if (!runtimeRoot) return;
        
        DestroyImmediate(runtimeRoot.gameObject);
        runtimeRoot = null;
    }

    void EnsureRuntimeRoot() {
        if (runtimeRoot) return;
        
        var root = new GameObject("BoidsRuntime");
        root.transform.SetParent(transform, false);
        runtimeRoot = root.transform;
    }

    BoidAgent CreateBoid(int index) {
        var spawnPosition = (Vector2)transform.position + GetRandomSpawnOffset();
        var spawnPosition3D = new Vector3(spawnPosition.x, spawnPosition.y, boidZ);
        var boidObject = Instantiate(boidPrefab, spawnPosition3D, Quaternion.identity, runtimeRoot);
        boidObject.name = $"Boid_{index:000}";
        boidObject.transform.localScale = Vector3.one * boidScale;
                
        var body = boidObject.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        var boid = boidObject.GetComponent<BoidAgent>();
        boid.ConfigureBounds(transform.position, flockBoundsSize, true);
        boid.ConfigureSpeed(baseSpeed + Random.Range(-speedVariance, speedVariance));
        boid.ConfigureDepth(boidZ);
        
        return boid;
    }
    
    Vector2 GetRandomSpawnOffset() => new(
        Random.Range(-spawnExtents.x, spawnExtents.x),
        Random.Range(-spawnExtents.y, spawnExtents.y)
    );

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, flockBoundsSize);
    }
}