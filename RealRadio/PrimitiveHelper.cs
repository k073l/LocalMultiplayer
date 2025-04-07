using System.Collections.Generic;
using UnityEngine;

namespace RealRadio;

public static class PrimitiveHelper
{
    private static Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

    public static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        if (!primitiveMeshes.ContainsKey(type))
        {
            CreatePrimitiveMesh(type);
        }

        return primitiveMeshes[type];
    }

    private static Mesh CreatePrimitiveMesh(PrimitiveType type)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(gameObject);

        primitiveMeshes[type] = mesh;
        return mesh;
    }
}
