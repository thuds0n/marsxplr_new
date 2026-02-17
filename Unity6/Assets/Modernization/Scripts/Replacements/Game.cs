using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game Controller;
    public static Settings Settings;
    public static Messaging Messaging;
    public static GameObject Player;
    public static GameObject QuarryVeh;

    public bool loadingWorld;
    public float worldLoadTime;

    private void Awake()
    {
        Controller = this;
        Settings = GetComponent<Settings>();
        Messaging = GetComponent<Messaging>();
        loadingWorld = false;
        worldLoadTime = Time.time;
        if (Settings != null && Settings.fogColor == Color.clear)
        {
            Settings.fogColor = RenderSettings.fogColor;
        }
    }

    private void Update()
    {
        if (Player == null)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) Player = tagged;
        }

        // Legacy reset lifecycle: /r style delayed reset back to base.
        if (Settings == null || Player == null) return;
        if (Settings.resetTime > 0f && (10f - (Time.time - Settings.resetTime)) < 1f)
        {
            var spawnPos = World.@base != null ? World.@base.position : Vector3.up * 6f;
            var spawnRot = World.@base != null ? World.@base.rotation : Quaternion.identity;

            Player.transform.position = spawnPos;
            Player.transform.rotation = spawnRot;

            var rb = Player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Settings.resetTime = -10f;
            Settings.updatePrefs();
        }
        else if (Settings.resetTime < -1f)
        {
            Settings.resetTime += Time.deltaTime;
        }
    }

    public void msg(string text, int origin)
    {
        if (Messaging != null)
        {
            Messaging.AddMessage(text, origin);
        }
    }
}
