using System;
using FishNet.Object;
using RealRadio.Components.Building;
using UnityEngine;

namespace RealRadio.Assets.Prefabs;

public class SingletonPrefabs
{
    public GameObject OffGridBuildManager { get; set; }

    public SingletonPrefabs()
    {
        OffGridBuildManager = CreateOffGridBuildManager();
    }

    private GameObject CreateOffGridBuildManager()
    {
        var prefab = AssetCreationUtil.CreatePrefabObject("@OffGridBuildManager");
        prefab.AddComponent<OffGridBuildManager>();
        prefab.AddComponent<NetworkObject>();

        AssetCreationUtil.RegisterPrefabForNetwork(prefab);
        return prefab;
    }
}
