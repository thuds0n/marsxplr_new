using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallReset : MonoBehaviour
{
    public float minY = -200f;
    public Vector3 fallbackSpawn = new Vector3(0f, 8f, 0f);

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (transform.position.y >= minY) return;

        Vector3 spawn = fallbackSpawn;
        if (World.@base != null)
        {
            spawn = World.@base.position + new Vector3(0f, 4f, 0f);
        }

        transform.position = spawn;
        transform.rotation = Quaternion.identity;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}
