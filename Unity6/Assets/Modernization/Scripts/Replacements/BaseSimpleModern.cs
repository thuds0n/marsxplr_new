using UnityEngine;

// Port of BaseSimple.js: the base marker's cylinders grow with camera
// distance so the base stays visible from afar, spin slowly, and scroll
// their texture.
public class BaseSimpleModern : MonoBehaviour
{
    public bool upMode;

    private Material _mat;

    private void Start()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr == null) return;
        _mat = mr.material;
        _mat.mainTextureScale = new Vector2(1f, 0.1f);
    }

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            float dist = Vector3.Distance(transform.position, cam.transform.position);
            transform.localScale = Vector3.one * Mathf.Max(0.5f, Mathf.Min(10f, dist / 10f));
        }

        Vector3 euler = transform.localEulerAngles;
        euler.y = Mathf.Repeat(euler.y + Time.deltaTime * 10f, 360f);
        transform.localEulerAngles = euler;

        if (_mat == null) return;
        Vector2 offset = _mat.mainTextureOffset;
        offset.x += Time.deltaTime * 0.5f;
        if (offset.x > 1f) offset.x--;

        if (upMode)
        {
            offset.y += Time.deltaTime * 0.1f;
            if (offset.y > 0.6f) upMode = false;
        }
        else
        {
            offset.y -= Time.deltaTime * 0.1f;
            if (offset.y < 0.4f) upMode = true;
        }
        _mat.mainTextureOffset = offset;
    }
}
