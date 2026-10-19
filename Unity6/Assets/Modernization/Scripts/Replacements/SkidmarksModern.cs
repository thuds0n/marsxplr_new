using UnityEngine;

// Port of Skidmarks.js (originally from Unity's networking sample): a ring of
// up to 256 tyre-track segments rebuilt into one world-space mesh.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SkidmarksModern : MonoBehaviour
{
    private const int MaxMarks = 256;
    // Interval at which new segments start; lower is smoother.
    private const float UpdateRate = 0.2f;

    public float markWidth = 0.225f;

    private class MarkSection
    {
        public Vector3 pos;
        public Vector3 normal;
        public Vector3 posl;
        public Vector3 posr;
        public float intensity;
        public int lastIndex = -1;
    }

    private readonly MarkSection[] _skidmarks = new MarkSection[MaxMarks];
    private int _numMarks;
    private float _updateTime;
    private bool _newTrackFlag = true;
    private bool _updateMeshFlag = true;
    private Mesh _mesh;

    private void Awake()
    {
        for (int i = 0; i < MaxMarks; i++) _skidmarks[i] = new MarkSection();
        _mesh = new Mesh { name = "Skidmarks" };
        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }

    // Adds a mark at pos connected to the segment lastIndex (-1 starts a new
    // track). Returns the index to pass as lastIndex next time.
    public int AddSkidMark(Vector3 pos, Vector3 normal, float intensity, int lastIndex)
    {
        intensity = Mathf.Clamp01(intensity);

        int currIndex = _numMarks;
        if (lastIndex != -1 && !_newTrackFlag) currIndex = lastIndex;

        MarkSection curr = _skidmarks[currIndex % MaxMarks];
        curr.pos = pos + normal * 0.05f - transform.position;
        curr.normal = normal;
        curr.intensity = intensity;

        if (lastIndex == -1 || _newTrackFlag) curr.lastIndex = lastIndex;

        if (curr.lastIndex != -1)
        {
            MarkSection last = _skidmarks[curr.lastIndex % MaxMarks];
            Vector3 xDir = Vector3.Cross(curr.pos - last.pos, normal).normalized;
            curr.posl = curr.pos + xDir * markWidth * 0.5f;
            curr.posr = curr.pos - xDir * markWidth * 0.5f;

            if (last.lastIndex == -1)
            {
                last.posl = curr.pos + xDir * markWidth * 0.5f;
                last.posr = curr.pos - xDir * markWidth * 0.5f;
            }
        }

        if (lastIndex == -1 || _newTrackFlag) _numMarks++;
        _updateMeshFlag = true;
        return currIndex;
    }

    private bool IsVisible(MarkSection mark)
    {
        return mark.lastIndex != -1 && mark.lastIndex > _numMarks - MaxMarks;
    }

    private void UpdateMesh()
    {
        int segmentCount = 0;
        for (int i = 0; i < _numMarks && i < MaxMarks; i++)
        {
            if (IsVisible(_skidmarks[i])) segmentCount++;
        }

        var vertices = new Vector3[segmentCount * 4];
        var normals = new Vector3[segmentCount * 4];
        var colors = new Color[segmentCount * 4];
        var uvs = new Vector2[segmentCount * 4];
        var triangles = new int[segmentCount * 6];
        segmentCount = 0;
        for (int i = 0; i < _numMarks && i < MaxMarks; i++)
        {
            MarkSection curr = _skidmarks[i];
            if (!IsVisible(curr)) continue;
            MarkSection last = _skidmarks[curr.lastIndex % MaxMarks];

            int v = segmentCount * 4;
            vertices[v] = last.posl;
            vertices[v + 1] = last.posr;
            vertices[v + 2] = curr.posl;
            vertices[v + 3] = curr.posr;

            normals[v] = normals[v + 1] = last.normal;
            normals[v + 2] = normals[v + 3] = curr.normal;

            colors[v] = colors[v + 1] = new Color(1f, 1f, 1f, last.intensity);
            colors[v + 2] = colors[v + 3] = new Color(1f, 1f, 1f, curr.intensity);

            uvs[v] = new Vector2(0f, 0f);
            uvs[v + 1] = new Vector2(1f, 0f);
            uvs[v + 2] = new Vector2(0f, 0f);
            uvs[v + 3] = new Vector2(1f, 0f);

            int t = segmentCount * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 1;
            triangles[t + 5] = v + 3;
            segmentCount++;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.normals = normals;
        _mesh.triangles = triangles;
        _mesh.colors = colors;
        _mesh.uv = uvs;
        _updateMeshFlag = false;
    }

    private void Update()
    {
        if (_updateMeshFlag) UpdateMesh();
    }

    private void FixedUpdate()
    {
        _newTrackFlag = false;
        _updateTime += Time.deltaTime;
        if (_updateTime > UpdateRate)
        {
            _newTrackFlag = true;
            _updateTime -= UpdateRate;
        }
    }
}
