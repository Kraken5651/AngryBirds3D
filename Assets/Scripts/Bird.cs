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

    [Header("VFX")]
    public GameObject spawnFX; // puff when bird appears on slingshot
    public GameObject deathFX; // feather burst when bird dies

    // ── State ─────────────────────────────────────
    protected bool      _launched    = false;
    protected bool      _hasHit      = false;
    protected bool      _hasBounced  = false;
    public    bool      _abilityUsed = false;
    public    Rigidbody _rb;
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

        // Spawn puff at birth
        if (spawnFX != null)
        {
            Instantiate(spawnFX, transform.position, Quaternion.identity);
            string spawnSound = birdType switch {
    BirdType.Chuck => "ChuckSpawn",
    BirdType.Bomb  => "BombSpawn",
    _              => "RedSpawn"
};
AudioManager.Instance?.Play(spawnSound);
        }
            
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
            string hitSound = birdType switch {
    BirdType.Chuck => "ChuckHit",
    BirdType.Bomb  => "BombHit",
    _              => "RedHit"
};
AudioManager.Instance?.Play(hitSound);
        }

        if (!_hasBounced)
        {
            _hasBounced = true;
            if (_rb != null)
            {
                Vector3 v = _rb.linearVelocity;
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

        // Spawn death VFX before destroying
        if (deathFX != null){
            Instantiate(deathFX, transform.position, Quaternion.identity);
            string deathSound = birdType switch {
    BirdType.Chuck => "ChuckDeath",
    BirdType.Bomb  => "BombDeath",
    _              => "RedDeath"
};
AudioManager.Instance?.Play(deathSound);
        }
    
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