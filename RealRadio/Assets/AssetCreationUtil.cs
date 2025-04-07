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
            PrefabsContainer.SetActive(false);
            Object.DontDestroyOnLoad(PrefabsContainer);
        }

        var go = new GameObject(name);
        go.transform.SetParent(PrefabsContainer.transform);
        go.hideFlags = HideFlags.HideAndDontSave;
        return go;
    }
}
