using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using ScheduleOne.Building;
using ScheduleOne.Interaction;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace RealRadio.Components.Building;

public class TogglableOffGridItem : OffGridItem
{
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStateToggled))]
    public bool IsOn { get; [ServerRpc(RequireOwnership = false, RunLocally = true)] set; }

    public override void Awake()
    {
        base.Awake();
    }

    private void OnInteractStart()
    {
        IsOn = !IsOn;
    }

    protected virtual void OnStateToggled(bool prev, bool next, bool asServer)
    {
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }
}
