using UnityEngine;

// The original buggy body collider: the convex "Collider" mesh from
// Martian Assets/Vehicles/Buggy/Components/Buggy.jas. The converted FBX lost
// this mesh, so its vertices and triangles are reproduced here. The original
// prefab scaled the model by 2.
public static class BuggyColliderShape
{
    private const float ModelScale = 2f;

    private static readonly float[] Vertices =
    {
        0f, 0.103f, 0.396f,      -0.175f, 0.084f, 0.391f,   0f, 0.252f, 0.007f,
        -0.411f, 0.035f, 0.312f, -0.202f, -0.102f, 0.314f,  0f, -0.102f, 0.35f,
        -0.345f, -0.102f, 0.265f, 0f, -0.102f, -0.453f,     -0.298f, -0.102f, -0.421f,
        -0.411f, 0.175f, -0.246f, -0.411f, 0.012f, -0.154f, 0f, 0.25f, -0.351f,
        0.411f, 0.175f, -0.246f, 0.411f, 0.012f, -0.154f,   0.411f, 0.035f, 0.312f,
        0.345f, -0.102f, 0.265f, 0.298f, -0.102f, -0.421f,  0.202f, -0.102f, 0.314f,
        0.175f, 0.084f, 0.391f,  0.188f, -0.043f, 0.425f,   0f, -0.048f, 0.48f,
        -0.188f, -0.043f, 0.425f, 0f, 0.249f, -0.475f,      0f, 0.074f, -0.52f,
        0.411f, 0.189f, -0.414f, -0.411f, 0.189f, -0.414f,
    };

    private static readonly int[] Triangles =
    {
        0, 1, 2, 3, 2, 1, 4, 5, 6, 5, 7, 6, 7, 8, 6, 8, 9, 10, 8, 10, 6, 11, 2, 3,
        3, 10, 11, 3, 6, 10, 9, 11, 10, 12, 13, 11, 14, 13, 15, 14, 11, 13, 11, 14, 2, 16, 15, 13,
        16, 13, 12, 7, 15, 16, 5, 15, 7, 17, 15, 5, 14, 18, 2, 0, 2, 18, 19, 15, 17, 17, 5, 19,
        20, 1, 0, 0, 18, 20, 21, 5, 4, 4, 6, 21, 5, 21, 20, 5, 20, 19, 18, 19, 20, 1, 20, 21,
        22, 8, 23, 23, 8, 7, 7, 16, 23, 23, 16, 22, 22, 16, 24, 24, 16, 12, 12, 11, 24, 24, 11, 22,
        9, 8, 25, 25, 8, 22, 22, 11, 25, 25, 11, 9, 3, 21, 6, 3, 1, 21, 15, 19, 14, 18, 14, 19,
    };

    private static Mesh _mesh;

    public static Mesh GetMesh()
    {
        if (_mesh != null) return _mesh;

        int count = Vertices.Length / 3;
        var points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = new Vector3(Vertices[i * 3], Vertices[i * 3 + 1], Vertices[i * 3 + 2]) * ModelScale;
        }

        _mesh = new Mesh { name = "BuggyColliderHull", vertices = points, triangles = Triangles };
        _mesh.RecalculateBounds();
        return _mesh;
    }
}
