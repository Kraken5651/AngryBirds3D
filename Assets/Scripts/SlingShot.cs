using UnityEngine;

public class Slingshot : MonoBehaviour
{
    [Header("References")]
    public GameObject   birdPrefab;
    public Transform    launchPoint;
    public LineRenderer trajectoryLine;
    public Camera       mainCamera;

    [Header("Tuning")]
    public float maxDragDistance = 2f;
    public float launchForce     = 15f;
    public int   trajectoryDots  = 25;
    public float timeStep        = 0.1f;

    [Header("Drag Cone")]
    [Range(10f, 160f)]
    public float  dragAngleLimit   = 60f;
    public Vector3 launchDirection = Vector3.right;

    [Header("Smoothing")]
    [Range(0.05f, 1f)]
    public float dragSmoothSpeed = 0.12f;  // lower = smoother/lazier

    // ── Private ───────────────────────────────────
    private GameObject _bird;
    private Rigidbody  _rb;
    private bool       _dragging   = false;
    private bool       _disabled   = false;   // true when birds run out
    private Vector3    _currentOffset  = Vector3.zero;
    private Vector3    _targetOffset   = Vector3.zero;
    private Vector3    _offsetVelocity = Vector3.zero; // for SmoothDamp

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        SpawnBird();
    }

    void Update()
    {
        if (_disabled) return;

        if (Input.GetMouseButtonDown(0))           BeginDrag();
        if (Input.GetMouseButton(0)  && _dragging) Drag();
        if (Input.GetMouseButtonUp(0) && _dragging) Launch();

        // Always smoothly animate bird position toward target
        if (_bird != null && _dragging)
        {
            _currentOffset = Vector3.SmoothDamp(
                _currentOffset,
                _targetOffset,
                ref _offsetVelocity,
                dragSmoothSpeed
            );
            _bird.transform.position = launchPoint.position + _currentOffset;
            DrawArc(_bird.transform.position, -_currentOffset * launchForce);
        }
    }

    void SpawnBird()
    {
        _bird = Instantiate(birdPrefab, launchPoint.position, Quaternion.identity);
        _rb   = _bird.GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _currentOffset  = Vector3.zero;
        _targetOffset   = Vector3.zero;
        _offsetVelocity = Vector3.zero;
        trajectoryLine.positionCount = 0;
    }

    void BeginDrag()
    {
        _dragging = true;
        _offsetVelocity = Vector3.zero;
    }

    void Drag()
    {
        // Mouse → world
        Vector3 m = Input.mousePosition;
        m.z = Vector3.Distance(mainCamera.transform.position, launchPoint.position);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(m);

        // Raw offset, projected flat
        Vector3 offset     = worldPos - launchPoint.position;
        Vector3 flatOffset = Vector3.ProjectOnPlane(offset, mainCamera.transform.forward);
        flatOffset         = Vector3.ClampMagnitude(flatOffset, maxDragDistance);

        // Cone constraint
        Vector3 pullBack = -launchDirection.normalized;
        float   angle    = Vector3.Angle(flatOffset, pullBack);
        if (angle > dragAngleLimit)
        {
            flatOffset = Vector3.RotateTowards(
                pullBack * flatOffset.magnitude,
                flatOffset,
                dragAngleLimit * Mathf.Deg2Rad,
                0f
            );
        }

        // Set as target — SmoothDamp in Update() chases this
        _targetOffset = flatOffset;
    }

    void Launch()
    {
        _dragging = false;
        trajectoryLine.positionCount = 0;

        // Launch using the smoothed current offset, not raw mouse
        Vector3 dir = -_currentOffset;

        _rb.isKinematic = false;
        _rb.AddForce(dir * launchForce, ForceMode.Impulse);

        _currentOffset  = Vector3.zero;
        _targetOffset   = Vector3.zero;
        _offsetVelocity = Vector3.zero;

        if (GameManager.Instance != null)
            GameManager.Instance.BirdLaunched();

        Invoke(nameof(SpawnBird), 2.5f);
    }

    // Called by GameManager when birds hit 0
    public void Disable()
    {
        _disabled = true;
        trajectoryLine.positionCount = 0;

        // Destroy the queued bird sitting on the slingshot
        if (_bird != null) Destroy(_bird);

        // Cancel any pending SpawnBird calls
        CancelInvoke(nameof(SpawnBird));
    }

    void DrawArc(Vector3 startPos, Vector3 startForce)
    {
        trajectoryLine.positionCount = trajectoryDots;
        Vector3 pos = startPos;
        Vector3 vel = startForce / _rb.mass;

        for (int i = 0; i < trajectoryDots; i++)
        {
            trajectoryLine.SetPosition(i, pos);
            vel += Physics.gravity * timeStep;
            pos += vel * timeStep;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (launchPoint == null) return;
        Vector3 pullBack = -launchDirection.normalized * maxDragDistance;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(launchPoint.position, pullBack);
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawRay(launchPoint.position,
            Quaternion.AngleAxis( dragAngleLimit, Vector3.up) * pullBack);
        Gizmos.DrawRay(launchPoint.position,
            Quaternion.AngleAxis(-dragAngleLimit, Vector3.up) * pullBack);
    }
}