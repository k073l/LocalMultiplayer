using UnityEngine;

namespace RealRadio.Assets;

public static class AssetCreationUtil
{
    private static GameObject? PrefabsContainer;

    public static GameObject CreatePrefabObject(string name)
    {
        if (PrefabsContainer == null)
        {
            PrefabsContainer = new GameObject("RealRadioPrefabs");
            Object.DontDestroyOnLoad(PrefabsContainer);
        }

        var go = new GameObject(name);
        go.transform.SetParent(PrefabsContainer.transform);
        go.SetActive(false);
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        return go;
    }
}
