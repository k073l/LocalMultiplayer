using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using UnityEngine;

namespace RealRadio.Components.Building;

public class TogglableOffGridItem : OffGridItem
{
    //[field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStateToggled))]
    //public bool IsOn { get; [ServerRpc(RequireOwnership = false, RunLocally = true)] set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStateToggled))]
    public bool IsOn { get; [ServerRpc(RequireOwnership = false, RunLocally = true)] set; }

    private float nextToggleTime = 0f;

    private void OnStateToggled(bool prev, bool next, bool asServer)
    {
        Plugin.Logger.LogInfo($"TogglableOffGridItem.IsOn changed from {prev} to {next} (asServer: {asServer})");
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);

        Plugin.Logger.LogInfo("TogglableOffGridItem.OnSpawnServer");
    }

    [ObserversRpc]
    private void BroadcastToClients(string message)
    {
        Plugin.Logger.LogInfo($"Incoming message: {message}");
    }

    private void Update()
    {
        if (isGhost)
            return;

        if (NetworkObject.IsServer)
        {
            if (Time.time >= nextToggleTime)
            {
                nextToggleTime = Time.time + 1f;

                var newValue = !IsOn;
                Plugin.Logger.LogInfo($"Toggling IsOn to {newValue}");
                IsOn = newValue;
                BroadcastToClients($"Toggled IsOn to {IsOn}");
            }
        }
    }
}
