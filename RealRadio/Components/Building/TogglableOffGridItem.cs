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

    private InteractableObject interactableObject = null!;

    public override void Awake()
    {
        base.Awake();

        interactableObject = GetComponentInChildren<InteractableObject>() ?? throw new InvalidOperationException("No InteractableObject component found in children");
        UpdateInteractionText();

        interactableObject.onInteractStart.AddListener(OnInteractStart);
    }

    private void OnInteractStart()
    {
        IsOn = !IsOn;
    }

    protected virtual void OnStateToggled(bool prev, bool next, bool asServer)
    {
        if (!asServer)
            UpdateInteractionText();
    }

    protected virtual void UpdateInteractionText()
    {
        if (IsOn)
            interactableObject.SetMessage("Turn off");
        else
            interactableObject.SetMessage("Turn on");
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
