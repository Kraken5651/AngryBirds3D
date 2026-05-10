using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class Slingshot : MonoBehaviour
{
    [Header("References")]
    public List<GameObject> birdTypePrefabs = new List<GameObject>();
    public Transform        launchPoint;
    public LineRenderer     trajectoryLine;
    public Camera           mainCamera;

    [Header("Tuning")]
    public float maxDragDistance = 2f;
    public float launchForce     = 15f;
    public int   trajectoryDots  = 30;
    public float timeStep        = 0.08f;

    [Header("Drag Cone")]
    [Range(10f, 160f)]
    public float   dragAngleLimit  = 60f;
    public Vector3 launchDirection = Vector3.right;
    // Add these fields in the Header("Drag Cone") section:
    [Header("Sideways Aim")]
    [Range(0f, 45f)]
    public float maxSidewaysAngle = 25f;   // how far left/right you can angle
    public float sidewaysSpeed    = 80f;   // degrees per second with A/D or Q/E

    [Header("Smoothing")]
    [Range(0.02f, 1f)]
    public float dragSmoothSpeed = 0.1f;

    [Header("Line Renderer")]
    public float lineWidth     = 0.08f;
    public Color lineColor     = new Color(1f, 1f, 1f, 0.85f);

    [Header("UI (optional)")]
    public TMP_Text birdTypeLabel;

    // ── Private ───────────────────────────────────
    private GameObject _bird;
    private Rigidbody  _rb;
    private bool       _dragging       = false;
    private bool       _disabled       = false;
    private bool       _waitingForNext = false;
    private Vector3    _currentOffset  = Vector3.zero;
    private Vector3    _targetOffset   = Vector3.zero;
    private Vector3    _offsetVelocity = Vector3.zero;
    private int        _selectedIndex  = 0;

    private float _sidewaysAngle = 0f;    // current horizontal aim offset

    public Transform CurrentBird => _bird != null ? _bird.transform : null;

    // Add this field at the top with other references:
    [Header("Camera")]
    public GameCameraController gameCamera; // drag camera GO here

    private readonly KeyCode[] _keys = {
        KeyCode.Alpha1, KeyCode.Alpha2,
        KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6
    };

    

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Configure LineRenderer properly at start
        if (trajectoryLine != null)
        {
            trajectoryLine.startWidth  = lineWidth;
            trajectoryLine.endWidth    = lineWidth * 0.5f;
            trajectoryLine.startColor  = lineColor;
            trajectoryLine.endColor    = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
            trajectoryLine.positionCount = 0;
            trajectoryLine.useWorldSpace = true;

            // Use a simple unlit material so it always renders visibly
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        SpawnBird();
    }

    void HandleSidewaysAim()
    {
        // A/D or Left/Right arrow to rotate aim angle
        float input = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  input -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input += 1f;

        _sidewaysAngle += input * sidewaysSpeed * Time.deltaTime;
        _sidewaysAngle  = Mathf.Clamp(_sidewaysAngle, -maxSidewaysAngle, maxSidewaysAngle);
    }



    void Update()
    {
        // R to restart — always works regardless of game state
        if (Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (Input.GetKeyDown(KeyCode.V))
            gameCamera?.TogglePip();

        if (_disabled) return;

        HandleTypeSwitch();

        if (!_waitingForNext) HandleSidewaysAim();

        HandleSidewaysAim();

        if (Input.GetMouseButtonDown(0))            BeginDrag();
        if (Input.GetMouseButton(0)   && _dragging) Drag();
        if (Input.GetMouseButtonUp(0) && _dragging) Launch();

        // Smooth bird position every frame while dragging
        if (_bird != null && _dragging && _rb != null)
        {
            _currentOffset = Vector3.SmoothDamp(
                _currentOffset, _targetOffset,
                ref _offsetVelocity, dragSmoothSpeed
            );
            _bird.transform.position = launchPoint.position + _currentOffset;

            // Always update arc while dragging
            UpdateArc();
        }
    }

    void HandleTypeSwitch()
{
    for (int i = 0; i < _keys.Length; i++)
    {
        if (!Input.GetKeyDown(_keys[i])) continue;

        // Find which prefabs are available in the remaining queue
        // Just preview — don't dequeue anything
        var queueList = new System.Collections.Generic.List<GameObject>(
            GameManager.Instance.SpawnQueue.ToArray());

        if (queueList.Count == 0) return;

        // Clamp index to available queue size
        int idx = Mathf.Clamp(i, 0, queueList.Count - 1);

        // Destroy current sitting bird and replace visually
        // WITHOUT touching the queue — just reorder queue front
        if (_bird != null && !_waitingForNext)
        {
            Destroy(_bird);
            _bird = null;
            _rb   = null;

            // Spawn the selected prefab visually only
            // Reinsert the current front back and swap with selected
            ReorderQueue(idx);
            SpawnBirdFromPrefab(queueList[idx]);
        }

        UpdateLabel();
        break;
    }
}

void ReorderQueue(int selectedIdx)
{
    // Pull queue into a list
    var list = new System.Collections.Generic.List<GameObject>(
        GameManager.Instance.SpawnQueue.ToArray());

    if (selectedIdx >= list.Count) return;

    // Swap selected to front without removing from queue
    GameObject selected = list[selectedIdx];
    list.RemoveAt(selectedIdx);
    list.Insert(0, selected);

    // Rebuild queue with new order
    GameManager.Instance.SpawnQueue.Clear();
    foreach (var prefab in list)
        GameManager.Instance.SpawnQueue.Enqueue(prefab);
}

void SpawnBirdFromPrefab(GameObject prefab)
{
    _bird = Instantiate(prefab, launchPoint.position, Quaternion.identity);
    _rb             = _bird.GetComponent<Rigidbody>();
    _rb.isKinematic = true;

    _currentOffset  = Vector3.zero;
    _targetOffset   = Vector3.zero;
    _offsetVelocity = Vector3.zero;
    _waitingForNext = false;

    gameCamera?.SetActiveBird(null);

    if (trajectoryLine != null)
        trajectoryLine.positionCount = 0;
}
    void SpawnBird()
    {
        // Check queue has birds remaining
        if (GameManager.Instance == null ||
            GameManager.Instance.SpawnQueue.Count == 0)
        {
            Disable();
            return;
        }

        // Dequeue next bird prefab
        GameObject prefab = GameManager.Instance.SpawnQueue.Dequeue();

        _bird = Instantiate(prefab, launchPoint.position, Quaternion.identity);
        _rb             = _bird.GetComponent<Rigidbody>();
        _rb.isKinematic = true;

        _currentOffset  = Vector3.zero;
        _targetOffset   = Vector3.zero;
        _offsetVelocity = Vector3.zero;
        _waitingForNext = false;

        gameCamera?.SetActiveBird(null);

        if (trajectoryLine != null)
        trajectoryLine.positionCount = 0;

        UpdateLabel();
    }

    void BeginDrag()
    {
        if (_bird == null) return;
        _dragging       = true;
        AudioManager.Instance?.Play("SlingshotDrag");
        _offsetVelocity = Vector3.zero;
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
                dragAngleLimit * Mathf.Deg2Rad, 0f
            );
        }

        Quaternion sidewaysRot = Quaternion.AngleAxis(_sidewaysAngle, Vector3.up);
        _targetOffset = sidewaysRot * flatOffset;
    }

    void Launch()
    {
        Bird launchedBird = _bird.GetComponent<Bird>();
        launchedBird?.OnLaunched();
        gameCamera?.SetActiveBird(_bird.transform);

        if (_bird == null || _rb == null) return;

        _dragging       = false;
        _waitingForNext = true;

        if (trajectoryLine != null)
            trajectoryLine.positionCount = 0;

        Vector3 dir = -_currentOffset;
        _rb.isKinematic = false;
        _rb.AddForce(dir * launchForce, ForceMode.Impulse);
        Bird b = _bird?.GetComponent<Bird>();
string launchSound = b?.birdType switch {
    BirdType.Chuck => "ChuckLaunch",
    BirdType.Bomb  => "BombLaunch",
    _              => "RedLaunch"
};
AudioManager.Instance?.Play(launchSound);

        _bird.GetComponent<Bird>()?.OnLaunched();

        gameCamera?.SetActiveBird(_bird != null ? _bird.transform : null);


        // Release references so flying bird is fully independent
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

    void UpdateLabel()
    {
        if (birdTypeLabel == null) return;
        string[] names = { "Red", "Chuck", "Bomb", "Blue" };
        int idx = Mathf.Clamp(_selectedIndex, 0, names.Length - 1);
        birdTypeLabel.text = $"[{idx + 1}] {names[idx]}";
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