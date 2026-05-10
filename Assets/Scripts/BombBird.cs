using UnityEngine;
using System.Collections;

public class BombBird : Bird
{
    [Header("Bomb")]
    public float      explosionRadius = 8f;
    public float      explosionForce  = 80f;
    public float      explosionDamage = 9999f;
    public float      fuseTime        = 0.2f;
    public GameObject explosionFXPrefab;
    // spawnFX inherited from Bird — assign in Inspector
    // deathFX intentionally unused — explosion handles it

    private bool _fuseStarted = false;

    protected override void Awake()
    {
        base.Awake(); // fires spawnFX automatically
        birdType = BirdType.Bomb;
    }

    protected override void UseAbility() => StartFuse();

    protected override void OnCollisionEnter(Collision col)
    {
        if (!_launched) return;
        float speed = col.relativeVelocity.magnitude;
        if (speed < minSpeedToHit) return;

        if (!_hasHit)
        {
            _hasHit = true;
            DealDamage(col.gameObject, baseDamage * speed * 0.25f);
        }

        if (!_fuseStarted) StartFuse();
    }

    void StartFuse()
    {
        if (_fuseStarted) return;
        _fuseStarted = true;
        _abilityUsed = true;
        StartCoroutine(FuseRoutine());
    }

    IEnumerator FuseRoutine()
    {
        Renderer rend    = GetComponent<Renderer>();
        float    elapsed = 0f;
        while (elapsed < fuseTime)
        {
            if (rend != null)
            {
                float t = Mathf.PingPong(elapsed * 25f, 1f);
                rend.material.color = Color.Lerp(Color.black, Color.red, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Explode();
    }

    void Explode()
    {
        Vector3    pos      = transform.position;
        float      radius   = explosionRadius;
        float      force    = explosionForce;
        float      damage   = explosionDamage;
        GameObject fxPrefab = explosionFXPrefab;

        Destroy(gameObject);
        AudioManager.Instance?.Play("BombExplosion");

        if (fxPrefab != null)
            Instantiate(fxPrefab, pos, Quaternion.identity);

        Collider[] hits     = new Collider[64];
        int        hitCount = Physics.OverlapSphereNonAlloc(pos, radius, hits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.gameObject == null) continue;

            Pig pig = hit.GetComponent<Pig>();
            if (pig != null)
            {
                pig.TakeDamage(damage);
                Rigidbody pigRb = hit.GetComponent<Rigidbody>();
                if (pigRb != null && !pigRb.isKinematic)
                    pigRb.AddForce(Vector3.up * force, ForceMode.Impulse);
                continue;
            }

            Structure s = hit.GetComponent<Structure>();
            if (s != null) { s.TakeDamage(damage * 0.8f); continue; }

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
                rb.AddExplosionForce(force * 5f, pos, radius, 0.5f, ForceMode.Impulse);
        }

        CameraShake.Instance?.Shake(0.5f, 0.35f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}