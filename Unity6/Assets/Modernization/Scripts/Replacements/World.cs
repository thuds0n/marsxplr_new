using UnityEngine;

public class World : MonoBehaviour
{
    public static int lodDist;
    public static Transform @base;
    public static Terrain[] terrains;
    public static Transform sea;
    public static Color seaFogColor;
    public static Color seaGlowColor;
    public static GameObject fallbackGround;

    public bool autoCreateFallbackGround = true;
    public float fallbackGroundSize = 2000f;
    public float fallbackGroundThickness = 8f;

    private void Awake()
    {
        if (@base == null)
        {
            var go = GameObject.Find("Base");
            if (go != null) @base = go.transform;
        }

        terrains = FindObjectsOfType<Terrain>();

        if (sea == null)
        {
            var seaGo = GameObject.Find("Sea");
            if (seaGo != null) sea = seaGo.transform;
        }

        if (seaFogColor == default) seaFogColor = new Color(0.6f, 0.2f, 0.1f, 1f);
        if (seaGlowColor == default) seaGlowColor = new Color(1f, 0.4f, 0.1f, 1f);

        if (autoCreateFallbackGround)
        {
            EnsureFallbackGround();
        }
    }

    private void EnsureFallbackGround()
    {
        if (terrains != null && terrains.Length > 0) return;
        if (fallbackGround != null) return;

        Vector3 probe = @base != null ? @base.position + Vector3.up * 50f : Vector3.up * 50f;
        if (Physics.Raycast(probe, Vector3.down, 300f, ~0, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        fallbackGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallbackGround.name = "FallbackGround";
        fallbackGround.transform.position = new Vector3(0f, -2f, 0f);
        if (@base != null)
        {
            fallbackGround.transform.position = new Vector3(@base.position.x, @base.position.y - 4f, @base.position.z);
        }

        fallbackGround.transform.localScale = new Vector3(fallbackGroundSize, fallbackGroundThickness, fallbackGroundSize);
        fallbackGround.layer = 8;

        var mr = fallbackGround.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.white;
            mat.mainTexture = CreateCheckerTexture();
            mat.mainTextureScale = new Vector2(180f, 180f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            mr.sharedMaterial = mat;
        }

        Debug.Log("[World] Created fallback ground for play-testing (no terrain detected).");
    }

    private static Texture2D CreateCheckerTexture()
    {
        const int size = 128;
        const int step = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "FallbackGroundChecker";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color a = new Color(0.53f, 0.28f, 0.18f, 1f);
        Color b = new Color(0.38f, 0.20f, 0.13f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool even = ((x / step) + (y / step)) % 2 == 0;
                tex.SetPixel(x, y, even ? a : b);
            }
        }

        tex.Apply(false, false);
        return tex;
    }
}
