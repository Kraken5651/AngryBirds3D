using UnityEngine;

public class Structure : MonoBehaviour
{
    public enum BlockType { Wood, Glass, Stone }

    [Header("Type")]
    public BlockType type = BlockType.Wood;

    [Header("Effects")]
    public GameObject breakFX; // particle prefab — assign in Inspector

    // ── Private ───────────────────────────────────
    private float _health;
    private float _maxHealth;
    private Renderer _rend;
    private Color _baseColor;
    private bool _broken = false;

    void Start()
    {
        _health    = GetMaxHealth();
        _maxHealth = _health;
        _rend      = GetComponent<Renderer>();

        if (_rend != null)
            _baseColor = _rend.material.color;
    }

    float GetMaxHealth()
    {
        return type switch
        {
            BlockType.Glass => 20f,
            BlockType.Stone => 250f,
            _               => 60f   // Wood
        };
    }

    float GetDamageMultiplier()
    {
        return type switch
        {
            BlockType.Glass => 3.0f,  // shatters easily
            BlockType.Stone => 0.1f,  // barely scratched
            _               => 1.0f   // wood normal
        };
    }

    public void TakeDamage(float amount)
    {
        if (_broken || amount <= 0f) return;

        _health -= amount * GetDamageMultiplier();
        _health  = Mathf.Max(0f, _health);

        UpdateVisual();

        if (_health <= 0f) Break();
    }

    void OnCollisionEnter(Collision col)
    {
        if (_broken) return;
        if (col.gameObject.CompareTag("Bird")) return;

        float speed = col.relativeVelocity.magnitude;
        if (speed < 2f) return;

        TakeDamage(speed * 3f);
    }

    void UpdateVisual()
    {
        if (_rend == null) return;

        // Darken progressively as health drops
        float t = 1f - (_health / _maxHealth);
        _rend.material.color = Color.Lerp(_baseColor, Color.black, t * 0.65f);
    }

    void Break()
    {
        if (_broken) return;
        _broken = true;

        if (breakFX != null)
            Instantiate(breakFX, transform.position, Quaternion.identity);

        int score = type switch
        {
            BlockType.Stone => 200,
            BlockType.Glass => 150,
            _               => 100
        };
        GameManager.Instance?.AddScore(score);

        string sfx = type switch {
            BlockType.Glass => "GlassBreak",
            _               => "WoodBreak"   // Wood and Stone both use wood thud
        };
        AudioManager.Instance?.Play(sfx);

        Destroy(gameObject);
    }
}