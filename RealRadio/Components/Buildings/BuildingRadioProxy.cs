using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using RealRadio.Components.Radio;
using ScheduleOne.Audio;
using ScheduleOne.Doors;
using ScheduleOne.Map;
using UnityEngine;

namespace RealRadio.Components.Buildings;

public class BuildingRadioProxy : RadioProxy
{
    public NPCEnterableBuilding? Building { get; set; }

    protected override void InitAudioClient(bool delayStart = true)
    {
        if (IsClientOnly && Building == null)
        {
            StartCoroutine(WaitForReceiveBuildingThenInitAudioClient());
            return;
        }

        base.InitAudioClient(delayStart);

        audioClient!.ConvertToMono = true;
    }

    private IEnumerator WaitForReceiveBuildingThenInitAudioClient()
    {
        yield return new WaitUntil(() => Building != null);
        InitAudioClient();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RequestBuildingInfo();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildingInfo(NetworkConnection conn = null!)
    {
        if (Building == null)
            throw new InvalidOperationException("Building is null");

        ReceiveBuildingInfo(conn, BuildingRadioManager.GetBuildingHash(Building));
    }

    [TargetRpc]
    private void ReceiveBuildingInfo(NetworkConnection conn, int buildingHash)
    {
        if (Building != null && IsClientOnly)
        {
            Plugin.Logger.LogWarning($"Received building again");
            return;
        }

        if (!BuildingRadioManager.Instance.Buildings.TryGetValue(buildingHash, out var building))
        {
            Plugin.Logger.LogError($"Building not found: {buildingHash}");
            return;
        }

        Building = building;
        OnBuildingSet();
    }

    private void OnBuildingSet()
    {
        if (Building == null)
            throw new InvalidOperationException("Building is null");

        name = $"{nameof(BuildingRadioProxy)} - {Building.name}";

        BuildingRadioManager.Instance.AddProxy(this);

        var parent = Building.GetComponentInChildren<StaticDoor>(includeInactive: true)?.gameObject;

        if (parent == null)
            parent = Building.gameObject;

        audioClientObject = Instantiate(AudioClientPrefab);
        audioClientObject.transform.SetParent(parent.transform, worldPositionStays: false);
        audioClientObject.SetActive(false);

        // Disable existing ambient loops in building
        var ambientSounds = Building.GetComponentsInChildren<AmbientLoop>(includeInactive: true).Select(x => (MonoBehaviour)x).Concat(Building.GetComponentsInChildren<AmbientLoopJukebox>(includeInactive: true));
        var ambientSounds = Building
            .GetComponentsInChildren<AmbientLoop>(includeInactive: true)
            .Select(x => (MonoBehaviour)x)
            .Concat(Building.GetComponentsInChildren<AmbientLoopJukebox>(includeInactive: true));

        foreach (var ambientSound in ambientSounds)
        {
            ambientSound.GetComponent<AudioSource>().mute = true;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        BuildingRadioManager.Instance.RemoveProxy(this);
    }
}
