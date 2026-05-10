using UnityEngine;

public class ChuckBird : Bird
{
    [Header("Chuck — Speed Boost")]
    public float speedMultiplier = 3.5f;
    public float gravityOffTime  = 0.35f;

    private TrailRenderer _trail;

    protected override void Awake()
    {
        base.Awake(); // fires spawnFX automatically
        birdType = BirdType.Chuck;

        _trail            = gameObject.AddComponent<TrailRenderer>();
        _trail.time       = 0.4f;
        _trail.startWidth = 0.3f;
        _trail.endWidth   = 0f;
        _trail.material   = new Material(Shader.Find("Sprites/Default"));
        _trail.startColor = new Color(1f, 0.85f, 0f, 0.9f);
        _trail.endColor   = new Color(1f, 0.85f, 0f, 0f);
        _trail.enabled    = false;
    }

    protected override void UseAbility()
    {
        if (_rb == null) return;
        Vector3 v = _rb.linearVelocity;
        if (v.sqrMagnitude < 0.01f) return;

        _rb.linearVelocity = v * speedMultiplier;
        AudioManager.Instance?.Play("ChuckBoost");
        _rb.useGravity     = false;
        _trail.enabled     = true;

        Invoke(nameof(RestoreGravity), gravityOffTime);
    }

    void RestoreGravity()
    {
        if (_rb == null) return;
        _rb.useGravity = true;
    }

    protected override void OnCollisionEnter(Collision col)
    {
        if (!_launched) return;

        float speed = col.relativeVelocity.magnitude;

        if (!_hasHit && speed >= minSpeedToHit)
        {
            _hasHit = true;
            float mult = _abilityUsed ? 2.2f : 1f;
            DealDamage(col.gameObject, baseDamage * speed * 0.25f * mult);
        }

        if (!_hasBounced)
        {
            _hasBounced = true;
            if (_rb != null)
            {
                Vector3 v = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(
                    v.x * 0.7f,
                    Mathf.Abs(v.y) * bounciness,
                    v.z * 0.7f);
            }
            Invoke(nameof(KillBounce), 0.3f);
        }
    }
}