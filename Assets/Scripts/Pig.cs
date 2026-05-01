using UnityEngine;

public class Pig : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Damage")]
    public float minImpactForce  = 3f;
    public float fallDamageScale = 5f;
    public float minFallSpeed    = 2f;

    [Header("Void Kill")]
    public float voidYThreshold = -8f;

    [Header("Effects")]
    public GameObject deathFX;

    private float     _health;
    private Renderer  _rend;
    private Color     _baseColor;
    private bool      _dead = false;
    private Rigidbody _rb;

    void Start()
    {
        _health    = maxHealth;
        _rend      = GetComponent<Renderer>();
        _baseColor = _rend.material.color;
        _rb        = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (_dead) return;
        if (transform.position.y < voidYThreshold)
            TakeDamage(maxHealth * 99f);
    }

    public void TakeDamage(float amount)
    {
        if (_dead || amount <= 0f) return;
        _health -= amount;
        _health  = Mathf.Max(0f, _health);
        UpdateColor();
        if (_health <= 0f) Die();
    }

    void OnCollisionEnter(Collision col)
    {
        if (_dead) return;
        if (col.gameObject.CompareTag("Bird")) return;

        float impactSpeed = col.relativeVelocity.magnitude;
        if (impactSpeed >= minFallSpeed)
            TakeDamage(impactSpeed * fallDamageScale);
    }

    void UpdateColor()
    {
        if (_rend == null) return;
        float t = 1f - Mathf.Clamp01(_health / maxHealth);
        _rend.material.color = Color.Lerp(_baseColor, Color.red, t);
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        if (deathFX != null)
            Instantiate(deathFX, transform.position, Quaternion.identity);
        GameManager.Instance?.PigKilled();
        Destroy(gameObject);
    }
}