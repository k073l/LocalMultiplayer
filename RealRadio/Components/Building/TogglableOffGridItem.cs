using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using UnityEngine;

namespace RealRadio.Components.Building;

public class TogglableOffGridItem : OffGridItem
{
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStateToggled))]
    public bool IsOn { get; [ServerRpc(RequireOwnership = false, RunLocally = true)] set; }

    protected virtual void OnStateToggled(bool prev, bool next, bool asServer)
    {
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Plugin.Logger.LogInfo($"IsOn: {IsOn}");
    }

    private void Update()
    {
        if (isGhost)
            return;


    }
}
