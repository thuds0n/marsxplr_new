using UnityEngine;

// Vehicle.js markers for vehicles other than the player: the Marker minimap
// marker, swapped for Marker Quarry while that vehicle is quarry.
[RequireComponent(typeof(VehicleStateModern))]
public class VehicleMarkersModern : MonoBehaviour
{
    private VehicleStateModern _vehicle;
    private GameObject _marker;
    private GameObject _markerQuarry;

    private void Start()
    {
        _vehicle = GetComponent<VehicleStateModern>();
        if (Game.Controller == null) return;
        if (Game.Controller.objectMarker != null)
        {
            _marker = Instantiate(Game.Controller.objectMarker, transform.position, transform.rotation);
            _marker.transform.parent = transform;
        }
        if (Game.Controller.objectMarkerQuarry != null)
        {
            _markerQuarry = Instantiate(Game.Controller.objectMarkerQuarry, transform.position, transform.rotation);
            _markerQuarry.transform.parent = transform;
        }
    }

    private void Update()
    {
        if (_markerQuarry == null || _marker == null) return;
        if (_vehicle.isIt != 0 && !_markerQuarry.activeSelf)
        {
            _markerQuarry.SetActive(true);
            _marker.SetActive(false);
        }
        else if (_vehicle.isIt == 0 && _markerQuarry.activeSelf)
        {
            _markerQuarry.SetActive(false);
            _marker.SetActive(true);
        }
    }
}
