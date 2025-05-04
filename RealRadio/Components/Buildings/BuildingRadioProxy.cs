using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using RealRadio.Components.Radio;
using ScheduleOne.Map;
using UnityEngine;

namespace RealRadio.Components.Buildings;

public class BuildingRadioProxy : RadioProxy
{
    public NPCEnterableBuilding? Building { get; set; }

    protected override void InitAudioClient(bool delayStart = true)
    {
        if (IsClient && Building == null)
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
        if (!BuildingRadioManager.Instance.Buildings.TryGetValue(buildingHash, out var building))
            throw new InvalidOperationException($"Building not found: {buildingHash}");

        Building = building;
        OnBuildingSet();
    }

    private void OnBuildingSet()
    {
        if (Building == null)
            throw new InvalidOperationException("Building is null");

        audioClientObject = Instantiate(AudioClientPrefab);
        audioClientObject.transform.SetParent(Building.transform, worldPositionStays: false);
        audioClientObject.SetActive(false);
    }
}
