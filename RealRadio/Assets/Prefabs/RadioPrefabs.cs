using FishNet.Component.Ownership;
using ScheduleOne;
using ScheduleOne.EntityFramework;
using UnityEngine;

namespace RealRadio.Assets.Prefabs;

public class RadioPrefabs
{
    public BuildableItem RadioTier1 { get; private set; }

    private readonly Registry registry;

    public RadioPrefabs(Registry registry)
    {
        RadioTier1 = CreateRadioPrefab();
        this.registry = registry;
    }

    private BuildableItem CreateRadioPrefab()
    {
        var go = AssetCreationUtil.CreatePrefabObject("RealRadioRadioTier1");
        var predictedSpawn = go.AddComponent<PredictedSpawn>();
        var buildable = go.AddComponent<BuildableItem>();
        // todo: set the rest of the fields
        return buildable;
    }
}
