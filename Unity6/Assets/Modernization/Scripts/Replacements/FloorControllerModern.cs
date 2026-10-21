using UnityEngine;

// Port of FloorController.js: when a world is generated, applies the world's
// "Texture" parameter (from the Floor's WhirldObject data) to the floor plane.
public class FloorControllerModern : MonoBehaviour
{
    public WhirldObjectModern whirldObject;
    public GameObject floorObject;

    private void OnSceneGenerated()
    {
        if (whirldObject == null || floorObject == null) return;
        whirldObject.Activate();
        if (whirldObject.Params == null || !whirldObject.Params.TryGetValue("Texture", out object value)) return;
        if (value is Texture texture) floorObject.GetComponent<Renderer>().material.mainTexture = texture;
    }
}
