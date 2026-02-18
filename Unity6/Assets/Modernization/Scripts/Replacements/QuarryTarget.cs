using UnityEngine;

public class QuarryTarget : MonoBehaviour
{
    public int scoreValue = 1;
    public bool spin = true;
    public float spinSpeed = 45f;

    private void Update()
    {
        if (!spin) return;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}
