using UnityEngine;

public class Bird : MonoBehaviour
{
    [Header("Damage")]
    public float baseDamage    = 50f;
    public float minSpeedToHit = 2f;

    [Header("Bounce")]
    public float bounciness        = 0.45f;  // 0 = no bounce, 1 = full bounce
    public float bounceSpeedCutoff = 1.5f;   // below this speed, stop bouncing
    public float postBounceGravity = 2.5f;   // heavier gravity after bounce so it rolls to stop

    private bool _hasHit     = false;
    private bool _hasBounced = false;
    private Rigidbody _rb;
    private PhysicsMaterial _bounceMat;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Build a physics material dynamically — one bounce only
        _bounceMat = new PhysicsMaterial("BirdBounce")
        {
            bounciness        = this.bounciness,
            bounceCombine     = PhysicsMaterialCombine.Maximum,
            frictionCombine   = PhysicsMaterialCombine.Minimum
        };
        GetComponent<Collider>().material = _bounceMat;
    }

    void OnCollisionEnter(Collision col)
    {
        float speed = col.relativeVelocity.magnitude;

        // ── Damage pass ────────────────────────────────────────────
        if (!_hasHit && speed >= minSpeedToHit)
        {
            _hasHit = true;
            float dmg = baseDamage * speed * 0.25f;

            Pig pig = col.gameObject.GetComponent<Pig>();
            if (pig != null) pig.TakeDamage(dmg);

            Structure s = col.gameObject.GetComponent<Structure>();
            if (s != null) s.TakeDamage(dmg * 0.6f);
        }

        // ── Bounce pass ────────────────────────────────────────────
        if (!_hasBounced)
        {
            _hasBounced = true;
            // After first bounce, kill the bounciness so it just rolls
            Invoke(nameof(KillBounce), 0.15f);
        }
    }

    void KillBounce()
    {
        // Remove bounce — bird now rolls/slides to a stop naturally
        _bounceMat.bounciness = 0f;
        GetComponent<Collider>().material = _bounceMat;

        // Increase gravity so it settles faster instead of rolling forever
        _rb.linearDamping     = 2f;
        _rb.angularDamping    = 3f;
        _rb.useGravity        = true;

        // Kill vertical velocity so it doesn't skip again
        Vector3 v = _rb.linearVelocity;
        v.y = Mathf.Min(v.y, 0f);
        _rb.linearVelocity = v;

        // Destroy after it settles
        Destroy(gameObject, 3f);
    }
}