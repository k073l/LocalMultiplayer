using System;
using UnityEngine;

namespace RealRadio;

[Flags]
public enum Layers
{
    Default = 1 << 0,
    TransparentFX = 1 << 1,
    IgnoreRaycast = 1 << 2,
    Unused1 = 1 << 3,
    Water = 1 << 4,
    UI = 1 << 5,
    Player = 1 << 6,
    Tile = 1 << 7,
    ViewModel = 1 << 8,
    Temporary = 1 << 9,
    Trash = 1 << 10,
    NPC = 1 << 11,
    WorldGrid = 1 << 12,
    Overlay = 1 << 13,
    WaterPipe = 1 << 14,
    Vehicle = 1 << 15,
    Pallet = 1 << 16,
    StoredItem = 1 << 17,
    Door = 1 << 18,
    Invisible = 1 << 19,
    IconGeneration = 1 << 20,
    VehicleObstacle = 1 << 21,
    Grass = 1 << 22,
    VehicleBlocker = 1 << 23,
    Terrain = 1 << 24,
    LightSource = 1 << 25,
    Task = 1 << 27,
    Glass = 1 << 28,
    Skateboard = 1 << 29,
    PlayerBlocker = 1 << 30,
    LOD = 1 << 31,
    Unused2 = 1 << 32,

    All =
        Default |
        TransparentFX |
        IgnoreRaycast |
        Unused1 |
        Water |
        UI |
        Player |
        Tile |
        ViewModel |
        Temporary |
        Trash |
        NPC |
        WorldGrid |
        Overlay |
        WaterPipe |
        Vehicle |
        Pallet |
        StoredItem |
        Door |
        Invisible |
        IconGeneration |
        VehicleObstacle |
        Grass |
        VehicleBlocker |
        Terrain |
        LightSource |
        Task |
        Glass |
        Skateboard |
        PlayerBlocker |
        LOD |
        Unused2
}

public static class LayersExtensions
{
    public static LayerMask ToLayerMask(this Layers layers)
    {
        return (int)layers;
    }
}
