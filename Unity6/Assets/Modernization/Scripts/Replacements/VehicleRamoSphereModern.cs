using UnityEngine;

// Port of the RORB ("ramo sphere") from Vehicle.js (OnPrefsUpdated, the
// ramoSphereScale step in FixedUpdate, OnRam) and RamoSphere.js. With the
// ramoSpheres server setting above 0, each vehicle carries a trigger sphere,
// grown to ramoSpheres × 15 + camOffset across, whose shield glows blue and
// lights up where two spheres meet. A vehicle whose sphere touches the quarry's rigidbody takes
// over as quarry ("rammed the Quarry"), with the same 3 s guards as laser tags.
// The ORB prefab values (layer 13, trigger radius 0.5, child "Sphere" using the
// built-in sphere mesh, tagColor and ramColor) were read with UnityPy from the
// shipped 2.22 sharedassets3. Not ported: XORBs (zorbBall, the solid rolling
// sphere and its ramColor).
[RequireComponent(typeof(VehicleStateModern))]
public class VehicleRamoSphereModern : MonoBehaviour
{
    private static readonly Color TagColor = new Color(0.03529412f, 0.5647059f, 1f, 0.2784314f);

    private VehicleStateModern _vehicle;
    private GameObject _ramoSphere;
    private Transform _shield;
    private Material _shieldMaterial;
    private float _ramoSphereScale;
    private float _offset;
    private float _appliedSetting = -1f;

    private void Awake()
    {
        _vehicle = GetComponent<VehicleStateModern>();
    }

    // Vehicle.js OnPrefsUpdated(): build, resize or remove the sphere when the
    // setting changes.
    private void Update()
    {
        Settings s = Game.Settings;
        if (s != null && s.ramoSpheres != _appliedSetting)
        {
            _appliedSetting = s.ramoSpheres;
            if (s.ramoSpheres != 0f)
            {
                if (_ramoSphere == null) Build();
                _ramoSphereScale = s.ramoSpheres * 15f + _vehicle.camOffset * 1f;
            }
            else if (_ramoSphere != null)
            {
                _ramoSphereScale = 0.1f;
                Destroy(_ramoSphere, 2f);
            }
        }
        if (_shield == null) return;

        // RamoSphere.js Update(): scroll the shield, ease its alpha back to
        // the tag colour's and keep it upright.
        _offset += Time.deltaTime * 0.1f;
        if (_offset > 1f) _offset -= Mathf.Floor(_offset);
        _shieldMaterial.SetFloat("_Offset", _offset);
        Color c = _shieldMaterial.color;
        c.a = Mathf.Lerp(c.a, TagColor.a, Time.deltaTime * 3f);
        _shieldMaterial.color = c;
        _shield.rotation = Quaternion.identity;
    }

    // Vehicle.js FixedUpdate(): ease the sphere towards its size.
    private void FixedUpdate()
    {
        if (_ramoSphereScale == 0f || _ramoSphere == null) return;
        Transform t = _ramoSphere.transform;
        t.localScale = Vector3.Lerp(t.localScale, Vector3.one * _ramoSphereScale, Time.fixedDeltaTime);
        if (t.localScale.x > _ramoSphereScale - 0.01f && t.localScale.x < _ramoSphereScale + 0.01f) _ramoSphereScale = 0f;
    }

    private void Build()
    {
        _ramoSphere = new GameObject("ORB");
        _ramoSphere.layer = 13;
        _ramoSphere.transform.SetParent(transform, false);
        _ramoSphere.transform.localScale = Vector3.zero;
        var sphere = _ramoSphere.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 0.5f;
        // Vehicle.js: the sphere ignores the vehicle's own colliders.
        foreach (Collider own in GetComponentsInChildren<Collider>(true))
        {
            if (own != sphere) Physics.IgnoreCollision(sphere, own);
        }
        _ramoSphere.AddComponent<RamoSphereTrigger>().owner = this;

        var assets = Resources.Load<RamoSphereAssets>("RamoSphereAssets");
        if (assets == null || assets.shield == null) return;
        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(shield.GetComponent<Collider>());
        shield.name = "Sphere";
        shield.transform.SetParent(_ramoSphere.transform, false);
        _shield = shield.transform;
        var r = shield.GetComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        // RamoSphere.js Start().
        _shieldMaterial = r.material = new Material(assets.shield);
        _shieldMaterial.color = TagColor;
    }

    // RamoSphere.js OnTriggerStay. Its impact flash (OnCollisionEnter) only
    // runs for a solid XORB sphere, so it is not ported.
    private void OnSphereStay(Collider other)
    {
        if (other.gameObject.layer == 14) return;
        // Another ORB inside ours lights the shield up.
        if (other.name == "ORB" && _shieldMaterial != null && _shieldMaterial.color.a < 3f)
        {
            Color c = _shieldMaterial.color;
            c.a = 3f;
            _shieldMaterial.color = c;
        }
        if (other.attachedRigidbody != null) OnRam(other.attachedRigidbody.gameObject);
    }

    // Vehicle.js OnRam(): we rammed the quarry, so we become quarry.
    private void OnRam(GameObject other)
    {
        var veh = other.GetComponent<VehicleStateModern>();
        if (veh == null || veh == _vehicle || veh.isIt != 1 || !veh.isResponding ||
            Time.time - _vehicle.lastTag < 3f || Time.time - veh.lastTag < 3f) return;
        _vehicle.lastTag = Time.time;
        if (GameModeSinglePlayerModern.Instance != null) GameModeSinglePlayerModern.Instance.SetQuarry(_vehicle, 1);
    }

    // Trigger messages go to the collider's own object, so the sphere passes
    // them up.
    private class RamoSphereTrigger : MonoBehaviour
    {
        public VehicleRamoSphereModern owner;

        private void OnTriggerStay(Collider other)
        {
            if (owner != null) owner.OnSphereStay(other);
        }
    }
}
