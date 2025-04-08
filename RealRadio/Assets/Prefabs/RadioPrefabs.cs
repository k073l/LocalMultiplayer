using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Component.Ownership;
using FishNet.Object;
using RealRadio.Components.Building;
using ScheduleOne;
using ScheduleOne.Building;
using ScheduleOne.EntityFramework;
using ScheduleOne.Equipping;
using ScheduleOne.Tiles;
using UnityEngine;

namespace RealRadio.Assets.Prefabs;

public class RadioPrefabs
{
    private const float GRID_TILE_SIZE = 0.45f;

    public BuildableItem RadioTier1 { get; private set; }

    public RadioPrefabs()
    {
        RadioTier1 = CreateRadioPrefab("radio_t1", Guid.Parse("b4718701-46ea-4da0-baa4-2537e5a663e4"), PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Cube));
    }

    private BuildableItem CreateRadioPrefab(string id, Guid guid, Mesh mesh)
    {
        var go = AssetCreationUtil.CreatePrefabObject("RadioTier1");
        go.AddComponent<PredictedSpawn>();
        var buildItem = go.AddComponent<OffGridItem>();

        buildItem.buildHandler = CreateOffGridBuildHandler(id).gameObject;

        var buildPoint = new GameObject("BuildPoint");
        buildPoint.transform.SetParent(buildItem.transform);
        buildPoint.transform.localPosition = Vector3.zero;
        buildItem.BuildPoint = buildPoint.transform;

        var MidAirCenterPoint = new GameObject("MidAirCenterPoint");
        MidAirCenterPoint.transform.SetParent(buildItem.transform);
        MidAirCenterPoint.transform.localPosition = new Vector3(0, GRID_TILE_SIZE / 2f, 0);
        buildItem.MidAirCenterPoint = MidAirCenterPoint.transform;

        var boundingCollider = new GameObject("BoundingCollider").AddComponent<BoxCollider>();
        boundingCollider.transform.SetParent(buildItem.transform);
        boundingCollider.isTrigger = true;
        boundingCollider.size = new Vector3(GRID_TILE_SIZE, GRID_TILE_SIZE, GRID_TILE_SIZE);
        boundingCollider.center = new Vector3(0, GRID_TILE_SIZE / 2f, 0);
        buildItem.BoundingCollider = boundingCollider;

        var meshFilter = new GameObject("Mesh").AddComponent<MeshFilter>();
        meshFilter.transform.SetParent(buildItem.transform);
        meshFilter.transform.localScale = Vector3.one * GRID_TILE_SIZE;
        meshFilter.transform.localPosition = new Vector3(0, GRID_TILE_SIZE / 2f, 0);
        meshFilter.sharedMesh = mesh;
        meshFilter.gameObject.AddComponent<MeshRenderer>();
        meshFilter.gameObject.AddComponent<MeshCollider>();

        AssetCreationUtil.RegisterPrefabForNetwork(go);
        return buildItem;
    }

    private BuildStart_Base CreateOffGridBuildHandler(string id)
    {
        var go = AssetCreationUtil.CreatePrefabObject($"OffGridBuildHandler_{id}");
        var buildStart = go.AddComponent<BuildStartOffGrid>();
        go.AddComponent<BuildUpdateOffGrid>();
        go.AddComponent<BuildStopOffGrid>();

        return buildStart;
    }
}
