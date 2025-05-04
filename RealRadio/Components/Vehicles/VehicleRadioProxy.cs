
using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using NAudio.CoreAudioApi;
using RealRadio.Components.Audio;
using RealRadio.Components.Radio;
using RealRadio.Data;
using ScheduleOne.Audio;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace RealRadio.Components.Vehicles;

public class VehicleRadioProxy : RadioProxy
{
    public LandVehicle Vehicle { get; set; } = null!;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Vehicle)
        {
            var proxyRef = Vehicle.gameObject.GetComponent<VehicleRadioProxyReference>();

            if (proxyRef)
                Destroy(proxyRef);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (IsServer)
        {
            if (Vehicle == null)
                throw new InvalidOperationException("Vehicle is null");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RequestVehicleInfo();
    }

    protected override void InitAudioClient(bool delayStart = true)
    {
        if (IsClient && Vehicle == null)
        {
            StartCoroutine(WaitForReceiveVehicleThenInitAudioClient());
            return;
        }

        base.InitAudioClient(delayStart);

        if (audioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null after caliing InitAudioClient");

        if (HasOccupants())
        {
            if (delayStart)
            {
                audioClientObject.SetActive(false);
                StartCoroutine(DelayedEnableAudioClientObject());
            }
            else
                audioClientObject.SetActive(true);
        }
        else
            audioClientObject.SetActive(false);

        UpdateAudioEffects();
    }

    private IEnumerator WaitForReceiveVehicleThenInitAudioClient()
    {
        if (IsServer)
            throw new InvalidOperationException("This coroutine should only run on clients");

        yield return new WaitUntil(() => Vehicle != null);
        InitAudioClient();
    }

    private IEnumerator DelayedEnableAudioClientObject()
    {
        yield return new WaitForSeconds(0.23f);
        audioClientObject?.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestVehicleInfo(NetworkConnection conn = null!)
    {
        ReceiveVehicleInfo(conn, Vehicle.gameObject);
    }

    [TargetRpc]
    private void ReceiveVehicleInfo(NetworkConnection conn, GameObject vehicleObject)
    {
        Vehicle = vehicleObject.GetComponent<LandVehicle>() ?? throw new InvalidOperationException($"Could not find LandVehicle component on received vehicle object ({vehicleObject?.name})");
        OnVehicleSet();
    }

    private void OnVehicleSet()
    {
        name = $"{name} ({Vehicle.name})";

        audioClientObject = Instantiate(AudioClientPrefab);
        audioClientObject.transform.SetParent(Vehicle.transform, worldPositionStays: false);
        audioClientObject.SetActive(false);

        Vehicle.onVehicleStart.AddListener(OnVehicleStart);
        Vehicle.onVehicleStop.AddListener(OnVehicleStop);
        Vehicle.onPlayerEnterVehicle += OnPlayerEnterVehicle;
        Vehicle.onPlayerExitVehicle += OnPlayerExitVehicle;

        var proxyRef = Vehicle.gameObject.AddComponent<VehicleRadioProxyReference>();
        proxyRef.Proxy = this;

        if (HasOccupants())
            OnEngineToggled(true);
        else
            OnEngineToggled(false);
    }

    private void Update()
    {
        if (Vehicle == null)
            return;

        if (IsServer)
        {
            if (!Vehicle)
            {
                Despawn(DespawnType.Destroy);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasOccupants()
    {
        return HasPlayerOccupants() || HasNPCOccupants();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasPlayerOccupants()
    {
        return Vehicle.OccupantPlayers.Count > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasNPCOccupants()
    {
        return Vehicle.OccupantNPCs.Any(x => x != null);
    }

    private void OnVehicleStart()
    {
        // The game currently calls OnVehicleStart after a client exits the vehicle
        // We can work around it by checking the number of occupants
        OnEngineToggled(HasOccupants());
    }

    private void OnVehicleStop()
    {
        // The game currently calls OnVehicleStart after a client exits the vehicle
        // We can work around it by checking the number of occupants
        OnEngineToggled(HasOccupants());
    }

    private void OnEngineToggled(bool started)
    {
        Plugin.Logger.LogInfo($"Engine toggled: {started}");

        if (IsServer)
        {
            if (started && HasNPCOccupants())
            {
                if (UnityEngine.Random.Range(0f, 1f) <= 0.5f)
                {
                    SetRadioStationIndex(RadioStationManager.Instance.GetRandomNPCStationIndex());
                }
                else
                {
                    SetRadioStationIndex(-1);
                }
            }
        }

        if (started)
        {
            if (!audioClient)
                InitAudioClient();
            else if (audioClientObject?.activeSelf == false)
                StartCoroutine(DelayedEnableAudioClientObject());
        }
        else
            audioClientObject?.SetActive(false);
    }

    private void OnPlayerEnterVehicle(Player player)
    {
        if (player == null || !player.IsLocalPlayer)
            return;

        if (RadioStation != null)
            UpdateAudioEffects();
    }

    private void OnPlayerExitVehicle(Player player)
    {
        if (player == null || !player.IsLocalPlayer)
            return;

        if (RadioStation != null)
            UpdateAudioEffects();
    }

    private void UpdateAudioEffects()
    {
        if (audioClient == null)
        {
            Plugin.Logger.LogWarning("Tried to update audio effects but audio client is null");
            return;
        }

        bool isInVehicle = Vehicle.OccupantPlayers.Contains(Player.Local);
        var source = audioClient.AudioSource;

        source.spatialBlend = isInVehicle ? 0 : 1;
        source.GetComponent<AudioLowPassFilter>().enabled = !isInVehicle;
        audioClient.ConvertToMono = !isInVehicle;
    }
}

public class VehicleRadioProxyReference : MonoBehaviour
{
    public VehicleRadioProxy? Proxy;
    public bool DestroyProxyOnDestroy = true;

    private void OnDestroy()
    {
        if (DestroyProxyOnDestroy && Proxy)
            Destroy(Proxy!.gameObject);
    }
}
