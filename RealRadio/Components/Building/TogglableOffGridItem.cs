using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using UnityEngine;

namespace RealRadio.Components.Building;

[RequireComponent(typeof(NetworkObject))]
public class TogglableOffGridItem : OffGridItem
{
    public bool IsOn => isOnSyncVar.GetValue(calledByUser: true);

    private SyncVar<bool> isOnSyncVar = null!;

    private float nextToggleTime = 0f;

    public override void Awake()
    {
        base.Awake();

        isOnSyncVar = new(this, syncIndex: 0, WritePermission.ClientUnsynchronized, ReadPermission.Observers, 0f, FishNet.Transporting.Channel.Reliable, value: false);
        isOnSyncVar.OnChange += OnStateToggled;
        isOnSyncVar.SetRegistered();

        // This is registered to handle reading ALL sync vars
        RegisterSyncVarRead(new FishNet.Object.Delegating.SyncVarReadDelegate(SyncVarRead));
    }

    private bool SyncVarRead(PooledReader reader, byte index, bool asServer)
    {
        switch (index)
        {
            case 0: // 0 = IsOn
                {
                    if (reader == null)
                    {
                        isOnSyncVar.SetValue(isOnSyncVar.GetValue(true), true);
                        return true;
                    }

                    isOnSyncVar.SetValue(reader.ReadBoolean(), true);
                    return true;
                }
        }

        return false;
    }

    private void OnStateToggled(bool prev, bool next, bool asServer)
    {
        Plugin.Logger.LogInfo($"TogglableOffGridItem.IsOn changed from {prev} to {next} (asServer: {asServer})");
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);

        Plugin.Logger.LogInfo("TogglableOffGridItem.OnSpawnServer");
    }

    private void Update()
    {
        if (isGhost)
            return;

        if (NetworkObject.IsServer)
        {
            if (Time.time >= nextToggleTime)
            {
                var newValue = !isOnSyncVar.GetValue(calledByUser: true);
                Plugin.Logger.LogInfo($"Toggling IsOn to {!newValue}");
                nextToggleTime = Time.time + 1f;
                isOnSyncVar.SetValue(newValue, calledByUser: true);
            }
        }
    }
}
