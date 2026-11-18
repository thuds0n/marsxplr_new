using System.Collections;
using UnityEngine;

// Port of the RORB ("ramo sphere") from Vehicle.js (OnPrefsUpdated, the
// ramoSphereScale step in FixedUpdate, OnRam) and RamoSphere.js. With the
// ramoSpheres server setting above 0, each vehicle carries a trigger sphere,
// grown to ramoSpheres × 15 + camOffset across, whose shield glows blue and
// lights up where two spheres meet. A vehicle whose sphere touches the quarry's rigidbody takes
// over as quarry ("rammed the Quarry"), with the same 3 s guards as laser tags.
// The ORB prefab values (layer 13, trigger radius 0.5, child "Sphere" using the
// built-in sphere mesh, tagColor and ramColor) were read with UnityPy from the
// shipped 2.22 sharedassets3. Settings.updatePrefs() calls OnPrefsUpdated on
// every vehicle, as Settings.js updateObjects() did. Not ported: XORBs
// (zorbBall, the solid rolling sphere and its ramColor).
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
    private Rigidbody _body;

    private void Awake()
    {
        _vehicle = GetComponent<VehicleStateModern>();
        _body = GetComponent<Rigidbody>();
    }

    // Vehicle.js Start() calls Settings.updateObjects(), which reaches
    // OnPrefsUpdated.
    private void Start()
    {
        OnPrefsUpdated();
    }

    public void OnPrefsUpdated()
    {
        StartCoroutine(PrefsUpdated());
    }

    // Vehicle.js OnPrefsUpdated(): a second after the preferences change,
    // build the sphere if there is none and grow it to its size, keeping the
    // rigidbody's inertia tensor and centre of mass; with RORBs off, shrink it
    // and destroy it 2 s later. As in the source, switching RORBs off and on
    // again within 2 s ends with no sphere until the next change.
    private IEnumerator PrefsUpdated()
    {
        Settings s = Game.Settings;
        if (s == null) yield break;
        if (s.ramoSpheres != 0f)
        {
            yield return new WaitForSeconds(1f);
            if (Game.Settings == null || Game.Settings.ramoSpheres == 0f) yield break;
            Vector3 tensor = _body != null ? _body.inertiaTensor : Vector3.zero;
            Vector3 cg = _body != null ? _body.centerOfMass : Vector3.zero;
            if (_ramoSphere == null) Build();
            _ramoSphereScale = Game.Settings.ramoSpheres * 15f + _vehicle.camOffset * 1f;
            if (_body != null)
            {
                _body.inertiaTensor = tensor;
                _body.centerOfMass = cg;
            }
        }
        else if (_ramoSphere != null)
        {
            _ramoSphereScale = 0.1f;
            GameObject sphere = _ramoSphere;
            yield return new WaitForSeconds(2f);
            if (sphere != null) Destroy(sphere);
        }
    }

    // Vehicle.js lR(): shrink the sphere while the vehicle rematerialises.
    public void Shrink()
    {
        if (_ramoSphere != null) _ramoSphereScale = 0.01f;
    }

    // Vehicle.js lR() and Game.js's delayed reset destroy the sphere; the next
    // OnPrefsUpdated builds a new one.
    public void DestroySphere()
    {
        if (_ramoSphere != null) Destroy(_ramoSphere);
        _ramoSphere = null;
        _shield = null;
        _shieldMaterial = null;
    }

    private void Update()
    {
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

    private void SetShieldAlpha(float a)
    {
        if (_shieldMaterial == null) return;
        Color c = _shieldMaterial.color;
        c.a = a;
        _shieldMaterial.color = c;
    }

    // RamoSphere.js OnTriggerEnter: another ORB entering flashes the shield.
    private void OnSphereEnter(Collider other)
    {
        if (other.gameObject.layer == 14) return;
        if (other.name == "ORB") SetShieldAlpha(10f);
    }

    // RamoSphere.js OnLaserHit: Rocket.js broadcasts the hit to the whole
    // vehicle, sphere included.
    private void OnShieldLaserHit()
    {
        SetShieldAlpha(5f);
    }

    // RamoSphere.js OnTriggerStay. Its impact flash (OnCollisionEnter) only
    // runs for a solid XORB sphere, so it is not ported.
    private void OnSphereStay(Collider other)
    {
        if (other.gameObject.layer == 14) return;
        // Another ORB inside ours lights the shield up.
        if (other.name == "ORB" && _shieldMaterial != null && _shieldMaterial.color.a < 3f) SetShieldAlpha(3f);
        if (other.attachedRigidbody != null) OnRam(other.attachedRigidbody.gameObject);
    }

    // Vehicle.js OnRam() is in VehicleLocalModern, shared with body contact.
    private void OnRam(GameObject other)
    {
        if (other == gameObject) return;
        var local = GetComponent<VehicleLocalModern>();
        if (local != null) local.OnRam(other);
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

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null) owner.OnSphereEnter(other);
        }

        // RocketModern broadcasts OnLaserHit with a bool, so take it here.
        private void OnLaserHit(bool isFatal)
        {
            if (owner != null) owner.OnShieldLaserHit();
        }
    }
}
