using UnityEngine;

public class Pig : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth      = 100f;
    public float currentHealth  = 100f;

    [Header("Impact")]
    public float minImpactForce = 5f;  // ignore tiny bumps

    [Header("Effects")]
    public GameObject deathFX; // particle prefab — assign in Inspector

    // ── Private ──────────────────────────────────
    private Renderer _rend;
    private Color    _baseColor;
    private bool     _dead = false;

    // ── Start ────────────────────────────────────
    void Start()
    {
        _rend      = GetComponent<Renderer>();
        _baseColor = _rend.material.color;
        currentHealth = maxHealth;
    }

    // ── Called by Bird.cs or other scripts ───────
    public void TakeDamage(float amount)
    {
        if (_dead) return;

        currentHealth -= amount;
        UpdateColor();

        if (currentHealth <= 0f) Die();
    }

    // ── Also react to physics impacts ────────────
    // (e.g. blocks falling on pig, or pig hitting wall)
    void OnCollisionEnter(Collision col)
    {
        if (_dead) return;

        // Don't double-count bird hits (Bird.cs handles those)
        if (col.gameObject.CompareTag("Bird")) return;

        float impact = col.impulse.magnitude;
        if (impact > minImpactForce)
            TakeDamage(impact * 0.5f);
    }

    // ── Visual: green -> yellow -> red ───────────
    void UpdateColor()
    {
        float t = 1f - Mathf.Clamp01(currentHealth / maxHealth);
        _rend.material.color = Color.Lerp(_baseColor, Color.red, t);
    }

    // ── Die ──────────────────────────────────────
    void Die()
    {
        _dead = true;

        // Spawn pop particle effect
        if (deathFX != null)
            Instantiate(deathFX, transform.position, Quaternion.identity);

        // Notify GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.PigKilled();

        Destroy(gameObject);
    }
}