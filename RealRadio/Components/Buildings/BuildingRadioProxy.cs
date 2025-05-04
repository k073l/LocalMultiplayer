using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using RealRadio.Components.Radio;
using ScheduleOne.Audio;
using ScheduleOne.Doors;
using ScheduleOne.GameTime;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Schedules;
using UnityEngine;

namespace RealRadio.Components.Buildings;

public class BuildingRadioProxy : RadioProxy
{
    public NPCEnterableBuilding? Building { get; set; }

    public int StopTime { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        ScheduleOne.GameTime.TimeManager.Instance.onMinutePass += OnMinutePass;
        ScheduleOne.GameTime.TimeManager.Instance.onDayPass += OnDayPass;
        ScheduleOne.GameTime.TimeManager.Instance._onSleepEnd.AddListener(OnSleepEnd);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        OnDayPass();
    }

    private void OnMinutePass()
    {
        if (IsClientOnly)
            return;

        if (ScheduleOne.GameTime.TimeManager.Instance.DailyMinTotal >= StopTime && RadioStationIndex != -1)
        {
            SetRadioStationIndex(-1);
        }
    }

    private void OnSleepEnd()
    {
        if (IsClientOnly)
            return;

        if (UnityEngine.Random.Range(0f, 1f) <= 0.5f)
        {
            StartCoroutine(DelayStartRandomStation());
        }
    }

    private IEnumerator DelayStartRandomStation()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(30f, 120f));

        if (Building?.OccupantCount is null or 0 || RadioStationIndex != -1)
            yield break;

        SetRadioStationIndex(RadioStationManager.Instance.GetRandomNPCStationIndex());
    }

    private void OnDayPass()
    {
        StopTime = 1440 - UnityEngine.Random.Range(1, 301);
    }

    protected override void InitAudioClient(bool delayStart = true)
    {
        if (IsClientOnly && Building == null)
        {
            StartCoroutine(WaitForReceiveBuildingThenInitAudioClient());
            return;
        }

        base.InitAudioClient(delayStart);

        audioClient!.ConvertToMono = true;
        audioClientObject!.SetActive(true);
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
