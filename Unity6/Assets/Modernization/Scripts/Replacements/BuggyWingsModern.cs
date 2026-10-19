using UnityEngine;

// Wing and trail presentation from Buggy.js Update: the wings unfold over half
// a second, then flex with roll input and the smoothed pitch input. The wing
// mesh is a per-vehicle copy, so the shared source mesh is never modified.
// Runs after the controller so it sees the current wing state.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BuggyControllerModern))]
public class BuggyWingsModern : MonoBehaviour
{
    // Copied from Buggy.prefab Lod0/Wings and its trails by SinglePlayerPlayableBootstrap.
    public MeshFilter wings;
    public Transform leftTrail;
    public Transform rightTrail;

    private BuggyControllerModern _controller;
    private VehicleStateModern _state;
    private Mesh _wingMesh;
    private Vector3[] _baseVertices;
    private Vector3[] _vertices;

    private void Start()
    {
        _controller = GetComponent<BuggyControllerModern>();
        _state = GetComponent<VehicleStateModern>();
        if (wings != null && wings.sharedMesh != null)
        {
            _baseVertices = wings.sharedMesh.vertices;
            _vertices = new Vector3[_baseVertices.Length];
            _wingMesh = Instantiate(wings.sharedMesh);
            _wingMesh.name = wings.sharedMesh.name + " (Animated)";
            _wingMesh.MarkDynamic();
            wings.sharedMesh = _wingMesh;
        }
    }

    private void OnDestroy()
    {
        if (_wingMesh != null) Destroy(_wingMesh);
    }

    private void Update()
    {
        bool open = _controller.wingOpen;
        float wingState = _controller.wingState;
        if (wings != null && wings.gameObject.activeSelf != open) wings.gameObject.SetActive(open);

        float trailX = 0f;
        if (open)
        {
            bool unfolding = wingState >= -1f && wingState < 0f || wingState >= 1f && wingState < 2f;
            // Opening runs 0 to 1; closing runs -1 to 0, which also flips the wings.
            float scale = wingState > 0f ? wingState - 1f : wingState;
            if (unfolding) trailX = wingState > 0f ? scale * -3.5f : scale * 3.5f;
            else trailX = -3.5f;

            if (_wingMesh != null)
            {
                float roll = _state.input.x;
                float pitch = _controller.motorInputSmoothed;
                for (int i = 0; i < _baseVertices.Length; i++)
                {
                    Vector3 pos = _baseVertices[i];
                    if (unfolding)
                    {
                        pos.x *= scale;
                        pos.y *= scale;
                    }
                    else
                    {
                        float t = pos.z * (roll * 0.14f);
                        if (pos.z > 0.2f) t = 0f;
                        else t *= Mathf.Abs(pos.x) / 10f;
                        t += pos.x * (pitch * 0.04f);
                        float st = Mathf.Sin(t);
                        float ct = Mathf.Cos(t);
                        // Buggy.js rotated y with the already-rotated x; kept for parity.
                        pos.x = pos.y * st + pos.x * ct;
                        pos.y = pos.y * ct - pos.x * st;
                    }
                    _vertices[i] = pos;
                }
                _wingMesh.vertices = _vertices;
            }
        }

        // trailX is the left trail's offset; the right trail mirrors it.
        SetTrailX(leftTrail, trailX);
        SetTrailX(rightTrail, -trailX);
    }

    private static void SetTrailX(Transform trail, float x)
    {
        if (trail == null) return;
        Vector3 p = trail.localPosition;
        p.x = x;
        trail.localPosition = p;
    }
}
