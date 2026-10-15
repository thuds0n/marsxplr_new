using UnityEngine;

// Placeholder hitscan laser for the local player, moved out of
// SimplePlayerController so it works with either vehicle controller. The
// original rocket and lock behaviour (VehicleMe.js, Rocket.js) is not yet ported.
public class PlayerLaserModern : MonoBehaviour
{
    private float _nextFireAt;
    private float _nextNoLaserMessageAt;

    private void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            TryFireLaser();
        }
    }

    private void TryFireLaser()
    {
        if (Game.Settings == null) return;
        if (!Game.Settings.lasersAllowed)
        {
            if (Game.Controller != null && Time.time >= _nextNoLaserMessageAt)
            {
                _nextNoLaserMessageAt = Time.time + 0.8f;
                Game.Controller.msg("Lasers disabled in settings.", 2);
            }
            return;
        }

        if (Time.time < _nextFireAt) return;
        _nextFireAt = Time.time + Mathf.Max(0.03f, Game.Settings.laserCooldown);

        float range = Mathf.Max(10f, Game.Settings.laserRange);
        float tunedSpeed = Mathf.Max(1f, Game.Settings.laserProjectileSpeed);
        float tunedGravity = Game.Settings.laserProjectileGravity;

        Camera cam = Camera.main;
        Ray ray = cam != null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

        if (Game.RaycastIgnoringPlayer(ray, range, out RaycastHit hit))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, Mathf.Min(0.2f, range / tunedSpeed));
            if (Game.Controller != null)
            {
                Game.Controller.RegisterLaserHit(hit.collider, hit.point, hit.normal, ray.direction);
            }
        }
        else
        {
            // Projectile speed and gravity are read now so values can be tuned before a full projectile implementation.
            Vector3 end = ray.origin + ray.direction * range + Vector3.down * (0.5f * tunedGravity * 0.04f);
            Debug.DrawLine(ray.origin, end, Color.red, Mathf.Min(0.2f, range / tunedSpeed));
        }
    }
}
