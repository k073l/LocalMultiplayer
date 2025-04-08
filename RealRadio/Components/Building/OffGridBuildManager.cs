using System;
using FishNet;
using FishNet.Object;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using UnityEngine;

namespace RealRadio.Components.Building;

public class OffGridBuildManager : Singleton<OffGridBuildManager>
{
    private NetworkObject networkObject = null!;

    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        gameObject.hideFlags = HideFlags.HideAndDontSave;

        networkObject = GetComponent<NetworkObject>() ?? throw new InvalidOperationException("No NetworkObject component found on game object");
    }

    private void OnEnable()
    {
        Plugin.Logger.LogInfo("** OffGridBuildManager enabled **");
    }

    private void OnDisable()
    {
        Plugin.Logger.LogInfo("** OffGridBuildManager disabled **");
    }

    public OffGridItem SpawnBuilding(ItemInstance itemInstance, Vector3 position, Quaternion rotation, Guid? id = null)
    {
        if (itemInstance.Definition is not BuildableItemDefinition itemDefinition)
        {
            throw new ArgumentException("itemInstance.Definition is not BuildableItemDefinition");
        }

        Guid guid = id ?? GUIDManager.GenerateUniqueGUID();
        OffGridItem item = Instantiate(itemDefinition.BuiltItem.gameObject).GetComponent<OffGridItem>();
        item.SetLocallyBuilt();
        item.InitializeOffGridItem(itemInstance, position, rotation, guid);
        networkObject.Spawn(item.gameObject);

        return item;
    }
}
