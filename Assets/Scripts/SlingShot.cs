using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Slingshot : MonoBehaviour
{
    [Header("References")]
    public Transform    launchPoint;
    public LineRenderer trajectoryLine;
    public Camera       mainCamera;

    [Header("Tuning")]
    public float maxDragDistance = 2f;
    public float launchForce     = 15f;
    public int   trajectoryDots  = 30;
    public float timeStep        = 0.08f;

    [Header("Drag Cone")]
    [Range(10f, 160f)]
    public float   dragAngleLimit  = 60f;
    public Vector3 launchDirection = Vector3.right;

    [Header("Sideways Aim")]
    [Range(0f, 45f)]
    public float maxSidewaysAngle = 25f;
    public float sidewaysSpeed    = 80f;

    [Header("Smoothing")]
    [Range(0.02f, 1f)]
    public float dragSmoothSpeed = 0.1f;

    [Header("Line Renderer")]
    public float lineWidth = 0.08f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.85f);

    [Header("Camera")]
    public GameCameraController gameCamera;

    // ── Private ───────────────────────────────────
    private GameObject _bird;
    private Rigidbody  _rb;
    private bool       _dragging       = false;
    private bool       _disabled       = false;
    private bool       _waitingForNext = false;
    private Vector3    _currentOffset  = Vector3.zero;
    private Vector3    _targetOffset   = Vector3.zero;
    private Vector3    _offsetVelocity = Vector3.zero;
    private float      _sidewaysAngle  = 0f;

    public Transform CurrentBird => _bird != null ? _bird.transform : null;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (trajectoryLine != null)
        {
            trajectoryLine.startWidth    = lineWidth;
            trajectoryLine.endWidth      = lineWidth * 0.5f;
            trajectoryLine.startColor    = lineColor;
            trajectoryLine.endColor      = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
            trajectoryLine.positionCount = 0;
            trajectoryLine.useWorldSpace = true;
            trajectoryLine.material      = new Material(Shader.Find("Sprites/Default"));
        }

        SpawnBird();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (_disabled) return;

        if (!_waitingForNext)
        {
            HandleTypeSwitch();
            HandleSidewaysAim();
        }

        if (Input.GetMouseButtonDown(0))            BeginDrag();
        if (Input.GetMouseButton(0)   && _dragging) Drag();
        if (Input.GetMouseButtonUp(0) && _dragging) Launch();

        if (_bird != null && _dragging && _rb != null)
        {
            _currentOffset = Vector3.SmoothDamp(
                _currentOffset, _targetOffset,
                ref _offsetVelocity, dragSmoothSpeed);
            _bird.transform.position = launchPoint.position + _currentOffset;
            UpdateArc();
        }
    }

    void HandleSidewaysAim()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  input -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input += 1f;
        _sidewaysAngle += input * sidewaysSpeed * Time.deltaTime;
        _sidewaysAngle  = Mathf.Clamp(_sidewaysAngle, -maxSidewaysAngle, maxSidewaysAngle);
    }

    void HandleTypeSwitch()
    {
        if (GameManager.Instance == null) return;

        // Map key to BirdType
        BirdType? requested = null;
        if (Input.GetKeyDown(KeyCode.Alpha1)) requested = BirdType.Red;
        if (Input.GetKeyDown(KeyCode.Alpha2)) requested = BirdType.Chuck;
        if (Input.GetKeyDown(KeyCode.Alpha3)) requested = BirdType.Bomb;

        if (requested == null) return;

        var list = new List<GameObject>(
            GameManager.Instance.SpawnQueue.ToArray());

        if (list.Count == 0) return;

        // Find first occurrence of requested type in queue
        int foundIdx = -1;
        for (int i = 0; i < list.Count; i++)
        {
            Bird b = list[i].GetComponent<Bird>();
            if (b != null && b.birdType == requested.Value)
            {
                foundIdx = i;
                break;
            }
        }

        // Type not in queue — do nothing
        if (foundIdx == -1) return;

        // Already at front — nothing to swap
        if (foundIdx == 0) return;

        // Swap found bird to front of queue
        GameObject selected = list[foundIdx];
        list.RemoveAt(foundIdx);
        list.Insert(0, selected);

        // Rebuild queue with new order
        GameManager.Instance.SpawnQueue.Clear();
        foreach (var p in list)
            GameManager.Instance.SpawnQueue.Enqueue(p);

        // Replace sitting bird with the swapped type
        if (_bird != null)
        {
            Destroy(_bird);
            _bird = null;
            _rb   = null;
        }

        InstantiateBird(selected);
    }

    void SpawnBird()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.SpawnQueue.Count == 0)
        {
            Disable();
            return;
        }

        GameObject prefab = GameManager.Instance.SpawnQueue.Dequeue();
        InstantiateBird(prefab);
        gameCamera?.SetActiveBird(null);

        if (trajectoryLine != null)
            trajectoryLine.positionCount = 0;
    }

    void InstantiateBird(GameObject prefab)
    {
        _bird           = Instantiate(prefab, launchPoint.position, Quaternion.identity);
        _rb             = _bird.GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _currentOffset  = Vector3.zero;
        _targetOffset   = Vector3.zero;
        _offsetVelocity = Vector3.zero;
        _waitingForNext = false;

        if (trajectoryLine != null)
            trajectoryLine.positionCount = 0;
    }

    void BeginDrag()
    {
        if (_bird == null) return;
        _dragging       = true;
        _offsetVelocity = Vector3.zero;
        AudioManager.Instance?.Play("SlingshotDrag");
    }

    void Drag()
    {
        if (mainCamera == null) return;

        Vector3 m = Input.mousePosition;
        m.z = Vector3.Distance(mainCamera.transform.position, launchPoint.position);
        Vector3 worldPos   = mainCamera.ScreenToWorldPoint(m);
        Vector3 offset     = worldPos - launchPoint.position;
        Vector3 flatOffset = Vector3.ProjectOnPlane(offset, mainCamera.transform.forward);
        flatOffset         = Vector3.ClampMagnitude(flatOffset, maxDragDistance);

        Vector3 pullBack = -launchDirection.normalized;
        float   angle    = Vector3.Angle(flatOffset, pullBack);
        if (angle > dragAngleLimit)
        {
            flatOffset = Vector3.RotateTowards(
                pullBack * flatOffset.magnitude,
                flatOffset,
                dragAngleLimit * Mathf.Deg2Rad, 0f);
        }

        _targetOffset = Quaternion.AngleAxis(_sidewaysAngle, Vector3.up) * flatOffset;
    }

    void Launch()
    {
        if (_bird == null || _rb == null) return;

        _dragging       = false;
        _waitingForNext = true;

        if (trajectoryLine != null)
            trajectoryLine.positionCount = 0;

        Bird   birdComp    = _bird.GetComponent<Bird>();
        string launchSound = birdComp?.birdType switch {
            BirdType.Chuck => "ChuckLaunch",
            BirdType.Bomb  => "BombLaunch",
            _              => "RedLaunch"
        };

        Vector3 dir     = -_currentOffset;
        _rb.isKinematic = false;
        _rb.AddForce(dir * launchForce, ForceMode.Impulse);

        birdComp?.OnLaunched();
        gameCamera?.SetActiveBird(_bird.transform);
        AudioManager.Instance?.Play(launchSound);

        _bird           = null;
        _rb             = null;
        _currentOffset  = Vector3.zero;
        _targetOffset   = Vector3.zero;
        _offsetVelocity = Vector3.zero;

        GameManager.Instance?.BirdLaunched();
        Invoke(nameof(SpawnBird), 2.5f);
    }

    void UpdateArc()
    {
        if (trajectoryLine == null || _rb == null) return;

        trajectoryLine.positionCount = trajectoryDots;
        Vector3 pos = launchPoint.position + _currentOffset;
        Vector3 vel = (-_currentOffset * launchForce) / _rb.mass;

        for (int i = 0; i < trajectoryDots; i++)
        {
            trajectoryLine.SetPosition(i, pos);
            vel += Physics.gravity * timeStep;
            pos += vel * timeStep;
        }
    }

    public void Disable()
    {
        _disabled = true;
        if (trajectoryLine != null) trajectoryLine.positionCount = 0;
        if (_bird != null) { Destroy(_bird); _bird = null; }
        _rb = null;
        CancelInvoke(nameof(SpawnBird));
    }

    void OnDrawGizmosSelected()
    {
        if (launchPoint == null) return;
        Vector3 pb = -launchDirection.normalized * maxDragDistance;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(launchPoint.position, pb);
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawRay(launchPoint.position,
            Quaternion.AngleAxis( dragAngleLimit, Vector3.up) * pb);
        Gizmos.DrawRay(launchPoint.position,
            Quaternion.AngleAxis(-dragAngleLimit, Vector3.up) * pb);
    }
}