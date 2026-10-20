using UnityEngine;

// The original laser aimer materials from Vehicle.prefab: Plasma Green while
// aiming, Plasma Red while locked on. The prefab's legacy particle components
// no longer load, so LegacyLaserSightController reads them from Resources.
public class LaserSightMaterials : ScriptableObject
{
    public Material aim;
    public Material aimLocked;
}
