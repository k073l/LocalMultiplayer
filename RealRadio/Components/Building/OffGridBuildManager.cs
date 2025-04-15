using System;
using FishNet;
using FishNet.Object;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using UnityEngine;

namespace RealRadio.Components.Building;

public class OffGridBuildManager : NetworkSingleton<OffGridBuildManager>
{
    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    /// <summary>
    /// Spawns a building. If this is the server, it will spawn the building instantly. Otherwise it will request the server to spawn the building.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if itemInstance.Definition is not BuildableItemDefinition.</exception>
    public void SpawnBuilding(ItemInstance itemInstance, Vector3 position, Quaternion rotation)
    {
        if (itemInstance.Definition is not BuildableItemDefinition itemDefinition)
        {
            throw new ArgumentException("itemInstance.Definition is not BuildableItemDefinition");
        }

        if (NetworkManager.IsServer)
        {
            Guid guid = GUIDManager.GenerateUniqueGUID();
            OffGridItem item = Instantiate(itemDefinition.BuiltItem.gameObject).GetComponent<OffGridItem>();
            item.SetLocallyBuilt();
            item.InitializeOffGridItem(itemInstance, position, rotation, guid);
            NetworkObject.Spawn(item.gameObject);
        }
        else
        {
            RequestSpawnBuilding(itemInstance, position, rotation);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnBuilding(ItemInstance itemInstance, Vector3 position, Quaternion rotation)
    {
        if (itemInstance == null)
            return;

        SpawnBuilding(itemInstance, position, rotation);
    }
}
