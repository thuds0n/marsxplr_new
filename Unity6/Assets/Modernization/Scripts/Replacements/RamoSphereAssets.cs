using UnityEngine;

// The RORB shield material (RamoShield, on the ForceField shader) from the
// original ORB prefab that Vehicle.prefab instantiates as ramoSphereObj. That
// prefab still carries the UnityScript RamoSphere.js, so VehicleRamoSphereModern
// rebuilds it and reads the material from Resources.
public class RamoSphereAssets : ScriptableObject
{
    public Material shield;
}
