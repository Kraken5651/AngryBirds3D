using UnityEngine;

public class Structure : MonoBehaviour
{
    public enum BlockType { Wood, Glass, Stone }

    [Header("Type & Health")]
    public BlockType type = BlockType.Wood;
    public float health = 50f;

    [Header("Effects")]
    public GameObject breakFX; // optional particle prefab

    private float _maxHealth;
    private Renderer _rend;
    private Color _baseColor;

    void Start()
    {
        _maxHealth = health;
        _rend = GetComponent<Renderer>();
        _baseColor = _rend.material.color;
    }

    public void TakeDamage(float amount)
    {
        // Stone resists damage, Glass is extra fragile
        float multiplier = type switch {
            BlockType.Glass => 2.0f,
            BlockType.Stone => 0.2f,
            _               => 1.0f
        };

        health -= amount * multiplier;
        UpdateVisual();

        if (health <= 0f) Break();
    }

    void OnCollisionEnter(Collision col)
    {
        float impact = col.impulse.magnitude;
        if (impact > 8f)
            TakeDamage(impact * 0.4f);
    }

    void UpdateVisual()
    {
        float t = 1f - Mathf.Clamp01(health / _maxHealth);
        _rend.material.color = Color.Lerp(_baseColor, Color.black, t * 0.5f);
    }

    void Break()
    {
        if (breakFX != null)
            Instantiate(breakFX, transform.position, Quaternion.identity);

        GameManager.Instance?.AddScore(100);
        Destroy(gameObject);
    }
}