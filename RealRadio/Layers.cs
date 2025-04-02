using System;
using UnityEngine;

namespace RealRadio;

[Flags]
public enum Layers
{
    Default,
    TransparentFX,
    IgnoreRaycast,
    Unused1,
    Water,
    UI,
    Player,
    Tile,
    ViewModel,
    Temporary,
    Trash,
    NPC,
    WorldGrid,
    Overlay,
    WaterPipe,
    Vehicle,
    Pallet,
    StoredItem,
    Door,
    Invisible,
    IconGeneration,
    VehicleObstacle,
    Grass,
    VehicleBlocker,
    Terrain,
    LightSource,
    Task,
    Glass,
    Skateboard,
    PlayerBlocker,
    LOD,
    Unused2,

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
