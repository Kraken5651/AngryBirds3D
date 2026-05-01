using UnityEngine;
using System.Collections;

public class BlueBird : Bird
{
    [Header("Blue — Split")]
    public GameObject splitPrefab;
    public int        splitCount  = 3;
    public float      spreadAngle = 20f;
    public float      speedBoost  = 1.1f;

    protected override void Awake()
    {
        base.Awake();
        birdType = BirdType.Blue;
    }

    protected override void UseAbility()
    {
        if (_rb == null) return;
        Vector3 vel = _rb.linearVelocity;
        if (vel.sqrMagnitude < 0.5f) return;

        float   speed = vel.magnitude * speedBoost;
        Vector3 dir   = vel.normalized;

        // Get the plane the bird is flying in
        // We want to spread UP and DOWN relative to flight direction
        // Cross with world-right to get the spread axis
        // This works for side-scrolling: birds fan above and below trajectory
        Vector3 spreadAxis;

        // If flying mostly horizontally, spread up/down
        if (Mathf.Abs(dir.x) > 0.3f || Mathf.Abs(dir.z) > 0.3f)
        {
            // Axis perpendicular to velocity in the vertical plane
            spreadAxis = Vector3.Cross(dir, mainCamera != null
                ? mainCamera.transform.right
                : Vector3.right).normalized;
        }
        else
        {
            spreadAxis = Vector3.right;
        }

        // Fallback safety
        if (spreadAxis.sqrMagnitude < 0.01f) spreadAxis = Vector3.up;

        // Calculate angles: for 3 birds → -20, 0, +20
        float startAngle = -((splitCount - 1) / 2f) * spreadAngle;

        for (int i = 0; i < splitCount; i++)
        {
            float   angle    = startAngle + i * spreadAngle;
            Vector3 splitDir = Quaternion.AngleAxis(angle, spreadAxis) * dir;

            if (i == 0)
            {
                // Redirect this bird along its new angle
                _rb.linearVelocity = splitDir.normalized * speed;
            }
            else
            {
                SpawnSplit(splitDir.normalized * speed);
            }
        }

        StartCoroutine(FlashWhite());
    }

    Camera mainCamera => Camera.main;

    void SpawnSplit(Vector3 velocity)
    {
        if (splitPrefab == null) return;

        // Spawn slightly offset so they don't overlap
        Vector3 spawnPos = transform.position + velocity.normalized * 0.3f;
        GameObject clone = Instantiate(splitPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = clone.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Critical: kinematic OFF before setting velocity
        rb.isKinematic    = false;
        rb.useGravity     = true;       // gravity ON so they arc naturally
        rb.linearVelocity = velocity;

        Bird b = clone.GetComponent<Bird>();
        if (b != null)
        {
            b.OnLaunched();             // enables collision damage + ability input
            b._abilityUsed = true;      // no chain split
            b.baseDamage   = baseDamage * 0.65f;
        }
    }

    IEnumerator FlashWhite()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) yield break;
        Color orig = rend.material.color;
        rend.material.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (rend != null) rend.material.color = orig;
    }
}