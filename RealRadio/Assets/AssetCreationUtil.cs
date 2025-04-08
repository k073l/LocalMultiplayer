using FishNet;
using FishNet.Managing.Object;
using FishNet.Object;
using UnityEngine;

namespace RealRadio.Assets;

public static class AssetCreationUtil
{
    // value was randomly chosen, but if another mod uses the same value it will not work most likely, not sure
    private const ushort SPAWNABLE_COLLECTION_ID = 5123;

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

    public static void RegisterPrefabForNetwork(GameObject prefab)
    {
        var netObj = prefab.GetComponent<NetworkObject>() ?? prefab.AddComponent<NetworkObject>();

        var prefabs = InstanceFinder.NetworkManager.GetPrefabObjects<SinglePrefabObjects>(SPAWNABLE_COLLECTION_ID, createIfMissing: true);
        prefabs.AddObject(netObj);
    }
}
