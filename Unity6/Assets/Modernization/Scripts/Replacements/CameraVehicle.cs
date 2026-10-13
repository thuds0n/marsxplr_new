using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraVehicle : MonoBehaviour
{
    public Transform arrow;
    public MotionBlur mb;
    public GlowEffect glowEffect;
    public ColorCorrectionEffect colorEffect;
    public float baseCamOffset = 4.2f;

    private float _rotationX;
    private float _rotationY;
    private Quaternion _gyroBaseRotation = Quaternion.identity;
    private Vector3 _lastDir = new Vector3(1f, 1f, 1f);
    private float _lastY;
    private float _heightBoost;
    private string _cameraToast = "";
    private float _cameraToastUntil;
    private bool _wasFirstPersonViewActive;

    private void Start()
    {
        Time.timeScale = 1f;
    }

    private void LateUpdate()
    {
        if (Game.Player == null) return;
        if (Game.Settings == null) return;

        var playerState = Game.Player.GetComponent<VehicleStateModern>();
        Transform rideTarget = playerState != null && playerState.rideTarget != null
            ? playerState.rideTarget
            : Game.Player.transform;

        bool chatting = Game.Messaging != null && Game.Messaging.chatting;
        bool firstPersonHold = Input.GetButton("Fire2") || (Input.GetButton("Snipe") && !chatting);
        bool firstPersonActive = Game.Settings.camMode == 0 || firstPersonHold;
        Cursor.lockState = firstPersonActive ? CursorLockMode.Locked : CursorLockMode.None;

        if (mb != null && mb.enabled)
        {
            mb.blurAmount -= Time.deltaTime * 0.13f;
            if (mb.blurAmount < 0f) mb.enabled = false;
        }

        ApplyHotkeys();
        float camDist = baseCamOffset + Game.Settings.camDist;
        if (camDist < 1f) camDist = 1f;

        if (firstPersonActive)
        {
            if (!_wasFirstPersonViewActive)
            {
                _rotationX = 0f;
                _rotationY = 0f;
                _gyroBaseRotation = Game.Settings.gyroCam
                    ? Quaternion.Euler(0f, Game.Player.transform.rotation.eulerAngles.y, 0f)
                    : Quaternion.identity;
            }

            transform.position = rideTarget.position + Vector3.up * 1.5f;

            if (Game.Settings.gyroCam)
            {
                if (_gyroBaseRotation == Quaternion.identity)
                {
                    _gyroBaseRotation = Quaternion.Euler(0f, Game.Player.transform.rotation.eulerAngles.y, 0f);
                }
                transform.rotation = _gyroBaseRotation;
            }
            else
            {
                transform.rotation = Game.Player.transform.rotation;
            }

            _rotationX += Input.GetAxis("Mouse X") * 2f;
            _rotationY += Input.GetAxis("Mouse Y") * 2f;
            _rotationY = Mathf.Clamp(_rotationY, Game.Settings.gyroCam ? -100f : -20f, 100f);
            transform.localRotation *= Quaternion.AngleAxis(_rotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(_rotationY, Vector3.left);
        }
        else if (Game.Settings.camMode == 2)
        {
            transform.position = Vector3.Lerp(transform.position, Game.Player.transform.position + (Vector3.up * 40f), Time.deltaTime * 0.3f);
            Quaternion wr = Quaternion.LookRotation(Game.Player.transform.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, wr, Time.deltaTime * 2f);
        }
        else if (Game.Settings.camMode == 3)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Game.Player.transform.position - transform.position), Time.deltaTime * 1.5f);
            transform.Translate(new Vector3(Input.GetAxis("camX") * Time.deltaTime * 50f, Input.GetAxis("camY") * Time.deltaTime * 40f, Input.GetAxis("camZ") * Time.deltaTime * 50f));
        }
        else
        {
            if (Game.Settings.camChase == 1)
            {
                var rb = Game.Player.GetComponent<Rigidbody>();
                if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f && rb.linearVelocity.normalized.y < 0.8f && rb.linearVelocity.normalized.y > -0.8f)
                {
                    _lastDir = Vector3.Lerp(_lastDir, rb.linearVelocity.normalized, 0.1f);
                }
                else
                {
                    _lastDir = Vector3.Lerp(_lastDir, new Vector3(_lastDir.x, 0f, _lastDir.z), 0.1f);
                }

                Vector3 newPos = rideTarget.position + _lastDir * -camDist + Vector3.up * (camDist / 3f);
                float chaseHeight = Mathf.Lerp(0.9f, 2.2f, Mathf.Clamp01(camDist / 30f));
                newPos.y = rideTarget.position.y + chaseHeight;
                transform.position += Vector3.up * ((rideTarget.position.y - _lastY) * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * 4f);
                _lastY = rideTarget.position.y;
            }
            else if (Game.Settings.camChase == 2)
            {
                var rb = Game.Player.GetComponent<Rigidbody>();
                if (rb != null && rb.linearVelocity.sqrMagnitude > 0f)
                {
                    float wantedRotationAngle = Quaternion.LookRotation(rb.linearVelocity).eulerAngles.y;
                    wantedRotationAngle += Input.GetAxis("Horizontal") * Mathf.Lerp(30f, 10f, camDist / 30f);
                    float wantedHeight = rideTarget.position.y + Mathf.Lerp(0.1f, 15f, camDist / 30f) + _heightBoost;
                    float targetHeight = Mathf.Lerp(0.5f, 3f, camDist / 30f);

                    float currentRotationAngle = transform.eulerAngles.y;
                    float currentHeight = transform.position.y;
                    currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, 3f * Time.deltaTime);
                    currentHeight = Mathf.Lerp(currentHeight, wantedHeight, 3f * Time.deltaTime);
                    Quaternion currentRotation = Quaternion.Euler(0f, currentRotationAngle, 0f);
                    Vector3 pos = rideTarget.position;
                    pos.y += targetHeight;
                    transform.position = pos;
                    transform.position -= currentRotation * Vector3.forward * camDist;
                    transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

                    if (Physics.Raycast(transform.position + (Vector3.up * 49.4f), Vector3.down, out var hit, 50f, 1 << 8) && !(hit.collider is TerrainCollider))
                    {
                        if (Physics.Linecast(transform.position, Game.Player.transform.position + (Vector3.up * currentHeight), out var hit2, 1 << 8))
                        {
                            transform.position += currentRotation * Vector3.forward * hit2.distance;
                            _heightBoost = hit2.distance * 0.7f;
                        }
                    }
                    else
                    {
                        _heightBoost = 0f;
                    }

                    transform.LookAt(pos);
                }
            }
            else
            {
                Vector3 target = rideTarget.position + Vector3.up * 1.2f;
                Vector3 desired = target - Game.Player.transform.forward * (camDist + 1.2f);
                transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 3.5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target - transform.position, Vector3.up), Time.deltaTime * 4f);
            }

            if (Physics.Linecast(transform.position + Vector3.up * 50f, transform.position + Vector3.down * 1f, out var groundHit, 1 << 8) && !(groundHit.collider is TerrainCollider))
            {
                transform.position += Vector3.up * (51f - groundHit.distance);
            }
        }

        if (arrow != null)
        {
            var quarry = Game.QuarryVeh != null ? Game.QuarryVeh.transform : (World.@base != null ? World.@base : Game.Player.transform);
            arrow.rotation = Quaternion.Lerp(arrow.rotation, Quaternion.LookRotation(quarry.position - Game.Player.transform.position), Time.deltaTime * 15f);
            arrow.localScale = Vector3.one * Mathf.Lerp(0.03f, 1f, Camera.main != null ? Camera.main.fieldOfView / 60f : 1f);
        }

        _wasFirstPersonViewActive = firstPersonActive;
    }

    private void ApplyHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Game.Settings.camMode = 0;
            _rotationX = 0f;
            _rotationY = 0f;
            _gyroBaseRotation = Game.Settings.gyroCam
                ? Quaternion.Euler(0f, Game.Player != null ? Game.Player.transform.rotation.eulerAngles.y : 0f, 0f)
                : Quaternion.identity;
            PlayerPrefs.SetInt("cam", 0);
            ShowCameraToast("Camera: First Person");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Game.Settings.camMode = 1;
            PlayerPrefs.SetInt("cam", 1);
            ShowCameraToast("Camera: Chase");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Game.Settings.camDist = 0f;
            Game.Settings.camMode = 1;
            PlayerPrefs.SetFloat("camDist", 0f);
            PlayerPrefs.SetInt("cam", 1);
            ShowCameraToast("Camera Distance: Near");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Game.Settings.camDist = 20f;
            Game.Settings.camMode = 1;
            PlayerPrefs.SetFloat("camDist", 20f);
            PlayerPrefs.SetInt("cam", 1);
            ShowCameraToast("Camera Distance: Far");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Game.Settings.camMode = 2;
            PlayerPrefs.SetInt("cam", 2);
            ShowCameraToast("Camera: Soar");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Game.Settings.camMode = 3;
            PlayerPrefs.SetInt("cam", 3);
            ShowCameraToast("Camera: Spectate");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Game.Settings.gyroCam = !Game.Settings.gyroCam;
            _rotationX = 0f;
            _rotationY = 0f;
            _gyroBaseRotation = Game.Settings.gyroCam
                ? Quaternion.Euler(0f, Game.Player != null ? Game.Player.transform.rotation.eulerAngles.y : 0f, 0f)
                : Quaternion.identity;
            PlayerPrefs.SetInt("gyroCam", Game.Settings.gyroCam ? 1 : 0);
            ShowCameraToast("Gyro Cam: " + (Game.Settings.gyroCam ? "On" : "Off"));
        }
    }

    private void ShowCameraToast(string text)
    {
        _cameraToast = text;
        _cameraToastUntil = Time.time + 1.4f;
        if (Game.Controller != null) Game.Controller.msg(text, 0);
    }

    private void OnPreRender()
    {
        if (Game.Settings == null || Game.Settings.fogColor == Color.clear) return;

        bool underwater = transform.position.y < Game.Settings.lavaAlt || Physics.Raycast(transform.position + Vector3.up * 200f, Vector3.down, 200f, 1 << 4);
        if (underwater)
        {
            RenderSettings.fogColor = World.seaFogColor;
            RenderSettings.fogDensity = Game.Settings.lavaFog;
            if (glowEffect != null) glowEffect.enabled = Game.Settings.renderLevel > 2;
            if (glowEffect != null) glowEffect.glowTint = World.seaGlowColor;
            if (Camera.main != null) Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
        else
        {
            RenderSettings.fogColor = Game.Settings.fogColor;
            RenderSettings.fogDensity = Game.Settings.worldFog;
            if (glowEffect != null) glowEffect.enabled = false;
            if (Camera.main != null) Camera.main.clearFlags = Camera.main.farClipPlane > 2000 ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        }

        if (colorEffect != null) colorEffect.enabled = glowEffect != null && glowEffect.enabled;
        if (Camera.main != null) Camera.main.backgroundColor = RenderSettings.fogColor;
    }

    private void OnGUI()
    {
        if (Time.time > _cameraToastUntil || string.IsNullOrEmpty(_cameraToast)) return;

        const int width = 320;
        const int height = 28;
        Rect rect = new Rect((Screen.width - width) * 0.5f, 12f, width, height);
        GUI.Box(rect, _cameraToast);
    }
}
