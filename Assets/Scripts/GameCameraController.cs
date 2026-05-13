using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    public enum CameraMode { Default, TopDown, Free }

    [Header("Mode")]
    public CameraMode currentMode = CameraMode.Default;

    [Header("Top Down")]
    public Vector3 topDownPosition = new Vector3(0f, 30f, 0f);
    public Vector3 topDownRotation = new Vector3(90f, 0f, 0f);
    public float   topDownFOV      = 75f;

    [Header("Free Camera")]
    public float freeMoveSpeed    = 15f;
    public float mouseSensitivity = 2f;

    [Header("Transition")]
    public float transitionSpeed = 5f;

    [Header("Landing Predictor")]
    public LineRenderer landingLine;
    public GameObject   landingMarker;
    public int          predictionSteps = 60;
    public float        predictionStep  = 0.1f;
    public LayerMask    collisionMask;

    // ── Private ───────────────────────────────────
    private Camera     _cam;
    private Vector3    _defaultPosition;
    private Quaternion _defaultRotation;
    private float      _defaultFOV;
    private Vector3    _targetPos;
    private Quaternion _targetRot;
    private float      _targetFOV;
    private float      _yaw, _pitch;
    private bool       _freeInit;
    private Transform  _activeBird;
    private Rigidbody  _activeBirdRb;
    private GameObject _landingMarkerInst;

    void Start()
    {
        _cam             = GetComponent<Camera>();
        _defaultPosition = transform.position;
        _defaultRotation = transform.rotation;
        _defaultFOV      = _cam.fieldOfView;
        _targetPos       = _defaultPosition;
        _targetRot       = _defaultRotation;
        _targetFOV       = _defaultFOV;

        SetupLandingPredictor();
    }

    void Update()
    {
        HandleInput();
        SmoothMove();
        UpdateLandingPredictor();
    }

    void HandleInput()
    {
        // Only F keys remain — no V, no 7/8/9
        if (Input.GetKeyDown(KeyCode.F1)) SetMode(CameraMode.Default);
        if (Input.GetKeyDown(KeyCode.F2)) SetMode(CameraMode.TopDown);
        if (Input.GetKeyDown(KeyCode.F3)) SetMode(CameraMode.Free);

        if (currentMode == CameraMode.Free) HandleFreeCamera();
    }

    void SetMode(CameraMode mode)
    {
        currentMode = mode;
        switch (mode)
        {
            case CameraMode.Default:
                _targetPos = _defaultPosition;
                _targetRot = _defaultRotation;
                _targetFOV = _defaultFOV;
                break;
            case CameraMode.TopDown:
                _targetPos = topDownPosition;
                _targetRot = Quaternion.Euler(topDownRotation);
                _targetFOV = topDownFOV;
                break;
            case CameraMode.Free:
                _freeInit  = false;
                _targetPos = transform.position;
                _targetRot = transform.rotation;
                _targetFOV = _cam.fieldOfView;
                break;
        }
    }

    void SmoothMove()
    {
        if (currentMode == CameraMode.Free) return;

        transform.position = Vector3.Lerp(transform.position, _targetPos,
                                          Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot,
                                              Time.deltaTime * transitionSpeed);
        _cam.fieldOfView   = Mathf.Lerp(_cam.fieldOfView, _targetFOV,
                                        Time.deltaTime * transitionSpeed);
    }

    void HandleFreeCamera()
    {
        if (!_freeInit)
        {
            _yaw      = transform.eulerAngles.y;
            _pitch    = transform.eulerAngles.x;
            _freeInit = true;
        }

        float spd = freeMoveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.W)) transform.Translate(Vector3.forward * spd, Space.Self);
        if (Input.GetKey(KeyCode.S)) transform.Translate(Vector3.back    * spd, Space.Self);
        if (Input.GetKey(KeyCode.A)) transform.Translate(Vector3.left    * spd, Space.Self);
        if (Input.GetKey(KeyCode.D)) transform.Translate(Vector3.right   * spd, Space.Self);
        if (Input.GetKey(KeyCode.E)) transform.Translate(Vector3.up      * spd, Space.Self);
        if (Input.GetKey(KeyCode.Q)) transform.Translate(Vector3.down    * spd, Space.Self);

        if (Input.GetMouseButton(2))
        {
            _yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        freeMoveSpeed = Mathf.Clamp(
            freeMoveSpeed + Input.GetAxis("Mouse ScrollWheel") * 20f, 2f, 80f);
    }

    // ── Landing predictor ─────────────────────────
    void SetupLandingPredictor()
    {
        if (landingLine == null) return;
        landingLine.positionCount = 0;
        landingLine.startWidth    = 0.05f;
        landingLine.endWidth      = 0.05f;
        landingLine.startColor    = new Color(1f, 0.3f, 0f, 0.7f);
        landingLine.endColor      = new Color(1f, 0.3f, 0f, 0f);
        landingLine.material      = new Material(Shader.Find("Sprites/Default"));
        landingLine.useWorldSpace = true;

        if (landingMarker != null)
        {
            _landingMarkerInst = Instantiate(landingMarker);
            _landingMarkerInst.SetActive(false);
        }
    }

    void UpdateLandingPredictor()
    {
        if (_activeBird == null || _activeBirdRb == null)
        {
            HidePredictor();
            return;
        }

        Vector3 pos = _activeBird.position;
        Vector3 vel = _activeBirdRb.linearVelocity;

        if (vel.sqrMagnitude < 0.5f)
        {
            HidePredictor();
            return;
        }

        Vector3[] points       = new Vector3[predictionSteps];
        Vector3   hitPoint     = Vector3.zero;
        bool      foundLanding = false;
        int       pointCount   = 0;

        for (int i = 0; i < predictionSteps; i++)
        {
            points[i]  = pos;
            pointCount = i + 1;

            vel += Physics.gravity * predictionStep;
            Vector3 nextPos = pos + vel * predictionStep;

            if (Physics.Raycast(pos, nextPos - pos,
                out RaycastHit hit,
                Vector3.Distance(pos, nextPos),
                collisionMask))
            {
                hitPoint     = hit.point;
                foundLanding = true;
                break;
            }

            pos = nextPos;
        }

        if (landingLine != null)
        {
            landingLine.positionCount = pointCount;
            landingLine.SetPositions(points);
        }

        if (_landingMarkerInst != null)
        {
            _landingMarkerInst.SetActive(foundLanding);
            if (foundLanding)
                _landingMarkerInst.transform.position = hitPoint + Vector3.up * 0.05f;
        }
    }

    void HidePredictor()
    {
        if (landingLine != null) landingLine.positionCount = 0;
        if (_landingMarkerInst != null) _landingMarkerInst.SetActive(false);
    }

    public void SetActiveBird(Transform bird)
    {
        _activeBird   = bird;
        _activeBirdRb = bird != null ? bird.GetComponent<Rigidbody>() : null;
        if (bird == null) HidePredictor();
    }
}