using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Bird : MonoBehaviour
{
    [Header("Type")]
    public BirdType birdType = BirdType.Red;

    [Header("Damage")]
    public float baseDamage    = 50f;
    public float minSpeedToHit = 2f;

    [Header("Bounce")]
    public float bounciness = 0.35f;

    // ── State ─────────────────────────────────────
    protected bool       _launched    = false;
    protected bool       _hasHit      = false;
    protected bool       _hasBounced  = false;
    public    bool       _abilityUsed = false;
    public    Rigidbody  _rb;
    private   PhysicsMaterial _bounceMat;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _bounceMat = new PhysicsMaterial("BirdBounce")
        {
            bounciness      = this.bounciness,
            bounceCombine   = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        GetComponent<Collider>().material = _bounceMat;
    }

    protected virtual void Update()
    {
        if (!_launched)                   return;
        if (_abilityUsed)                 return;
        if (!Input.GetMouseButtonDown(1)) return;

        _abilityUsed = true;
        UseAbility();
    }

    public void OnLaunched() => _launched = true;

    protected virtual void UseAbility() { }

    // Central damage dealer — calls TakeDamage directly on Pig
    // This bypasses OnCollisionEnter on Pig so there's no double-hit
    protected void DealDamage(GameObject target, float dmg)
    {
        if (target == null) return;
        target.GetComponent<Pig>()?.TakeDamage(dmg);
        target.GetComponent<Structure>()?.TakeDamage(dmg * 0.6f);
    }

    protected virtual void OnCollisionEnter(Collision col)
    {
        if (!_launched) return;

        float speed = col.relativeVelocity.magnitude;

        if (!_hasHit && speed >= minSpeedToHit)
        {
            _hasHit = true;
            DealDamage(col.gameObject, baseDamage * speed * 0.25f);
        }

        if (!_hasBounced)
        {
            _hasBounced = true;
            if (_rb != null)
            {
                Vector3 v = _rb.linearVelocity;
                // Preserve horizontal momentum, flip vertical with bounciness
                _rb.linearVelocity = new Vector3(
                    v.x * 0.75f,
                    Mathf.Abs(v.y) * bounciness,
                    v.z * 0.75f);
            }
            Invoke(nameof(KillBounce), 0.35f);
        }
    }

    protected void KillBounce()
    {
        if (this == null || gameObject == null) return;
        if (_bounceMat != null)
        {
            _bounceMat.bounciness         = 0f;
            GetComponent<Collider>().material = _bounceMat;
        }
        if (_rb != null)
        {
            _rb.linearDamping  = 2f;
            _rb.angularDamping = 3f;
        }
        Destroy(gameObject, 3f);
    }
}