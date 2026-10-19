using UnityEngine;

// Wheel, axle, and tyre-track presentation from Buggy.js, driven by the
// contact state BuggyControllerModern publishes each physics step.
// Runs after the controller so skidmarks use the current step's contacts.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BuggyControllerModern))]
public class BuggyPresentationModern : MonoBehaviour
{
    public GameObject wheelPrefab;
    public GameObject axelPrefab;
    public Material skidmarkMaterial;
    // Buggy.prefab: axelPos (0.375, -0.1, 0.75).
    public Vector3 axelPos = new Vector3(0.375f, -0.1f, 0.75f);

    private BuggyControllerModern _controller;
    private readonly Transform[] _wheels = new Transform[4];
    private readonly Transform[] _wheelGraphics = new Transform[4];
    private readonly Transform[] _axels = new Transform[4];
    private readonly int[] _wheelMarkIndex = { -1, -1, -1, -1 };
    private SkidmarksModern _wheelMarks;

    private void Start()
    {
        _controller = GetComponent<BuggyControllerModern>();
        Vector3 wheelPos = _controller.wheelPos;

        for (int i = 0; i < 4; i++)
        {
            float side = i % 2 == 1 ? 1f : -1f;
            float end = i < 2 ? 1f : -1f;

            if (wheelPrefab != null)
            {
                GameObject wheel = Instantiate(wheelPrefab, transform);
                wheel.transform.localPosition = new Vector3(wheelPos.x * side, wheelPos.y, wheelPos.z * end);
                wheel.transform.localRotation = Quaternion.identity;
                _wheels[i] = wheel.transform;
                _wheelGraphics[i] = wheel.transform.Find("Graphic");

                Transform simple = wheel.transform.Find("Graphic/Simple");
                if (simple != null)
                {
                    if (i == 1 || i == 3) simple.localEulerAngles = new Vector3(simple.localEulerAngles.x, 0f, simple.localEulerAngles.z);
                    // Render level 0: detailed wheels and axles only.
                    simple.gameObject.SetActive(false);
                }
            }

            if (axelPrefab != null)
            {
                GameObject axel = Instantiate(axelPrefab, transform);
                axel.transform.localPosition = new Vector3(axelPos.x * side, axelPos.y, axelPos.z * end);
                axel.transform.localRotation = Quaternion.identity;
                _axels[i] = axel.transform;
            }
        }

        // Tyre tracks are drawn for the player only, as in Buggy.js.
        if (skidmarkMaterial != null && CompareTag("Player"))
        {
            var go = new GameObject("Buggy Skidmarks", typeof(MeshFilter), typeof(MeshRenderer));
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = skidmarkMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _wheelMarks = go.AddComponent<SkidmarksModern>();
        }
    }

    private void OnDestroy()
    {
        if (_wheelMarks != null) Destroy(_wheelMarks.gameObject);
    }

    private void FixedUpdate()
    {
        if (_wheelMarks == null) return;
        for (int i = 0; i < 4; i++)
        {
            if (_controller.hitDistance[i] == -1f)
            {
                _wheelMarkIndex[i] = -1;
                continue;
            }
            _wheelMarkIndex[i] = _wheelMarks.AddSkidMark(_controller.contactPoint[i], _controller.contactNormal[i],
                _controller.skidIntensity[i], _wheelMarkIndex[i]);
        }
    }

    private void Update()
    {
        Vector3 wheelPos = _controller.wheelPos;
        float circumference = _controller.wheelRadius * Mathf.PI * 2f;

        for (int i = 0; i < 4; i++)
        {
            if (_wheels[i] == null) continue;

            float hit = _controller.hitDistance[i];
            _wheels[i].localPosition = new Vector3(
                wheelPos.x * (i % 2 == 1 ? 1 : -1),
                wheelPos.y - (hit == -1f ? _controller.suspensionRange : hit - _controller.wheelRadius),
                wheelPos.z * (i < 2 ? 1 : -1));
            _wheels[i].localRotation = _controller.wheelLocalRotation[i];

            if (_wheelGraphics[i] != null)
            {
                _wheelGraphics[i].Rotate(360f * (_controller.motorSpeed / circumference) * Time.deltaTime * 0.5f, 0f, 0f);
            }
            if (_axels[i] != null) _axels[i].LookAt(_wheels[i].position);
        }
    }
}
