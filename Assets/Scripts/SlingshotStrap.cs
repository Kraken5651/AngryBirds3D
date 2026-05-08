using UnityEngine;

public class SlingshotStrap : MonoBehaviour
{
    [Header("Fork Points")]
    public Transform forkLeft;    // left prong tip of slingshot
    public Transform forkRight;   // right prong tip of slingshot

    [Header("Strap Renderers")]
    public LineRenderer strapLeft;  // line from left fork to bird
    public LineRenderer strapRight; // line from right fork to bird

    [Header("Strap Look")]
    public float strapWidth   = 0.06f;
    public Color strapColor   = new Color(0.45f, 0.28f, 0.08f, 1f); // brown

    [Header("Slingshot Reference")]
    public Slingshot slingshot; // to read bird position

    private Transform _bird => slingshot != null ? GetBirdTransform() : null;

    void Start()
    {
        SetupLine(strapLeft);
        SetupLine(strapRight);
    }

    void LateUpdate()
    {
        // LateUpdate so bird has already been moved this frame
        Transform bird = _bird;

        if (bird == null || forkLeft == null || forkRight == null)
        {
            // No bird — draw strap between forks (resting position)
            Vector3 mid = forkLeft != null && forkRight != null
                ? (forkLeft.position + forkRight.position) * 0.5f
                : Vector3.zero;

            DrawStrap(strapLeft,  forkLeft?.position  ?? mid, mid);
            DrawStrap(strapRight, forkRight?.position ?? mid, mid);
            return;
        }

        // Draw from each fork tip to the bird center
        DrawStrap(strapLeft,  forkLeft.position,  bird.position);
        DrawStrap(strapRight, forkRight.position, bird.position);
    }

    void SetupLine(LineRenderer lr)
    {
        if (lr == null) return;
        lr.positionCount = 2;
        lr.startWidth    = strapWidth;
        lr.endWidth      = strapWidth * 0.7f;
        lr.startColor    = strapColor;
        lr.endColor      = strapColor;
        lr.useWorldSpace = true;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
    }

    void DrawStrap(LineRenderer lr, Vector3 from, Vector3 to)
    {
        if (lr == null) return;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }

    // Grab the current bird sitting on the slingshot via reflection
    // without needing to expose private fields
    Transform GetBirdTransform()
    {
        // Access via public property we add to Slingshot
        return slingshot.CurrentBird;
    }
}