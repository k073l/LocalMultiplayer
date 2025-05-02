
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RealRadio.Components.Audio;
using RealRadio.Components.Radio;
using RealRadio.Data;
using ScheduleOne.Audio;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace RealRadio.Components.Vehicles;

public class VehicleRadioProxy : NetworkBehaviour
{
    [field: SerializeField]
    public GameObject AudioClientPrefab { get; private set; } = null!;

    public RadioStation? RadioStation { get; private set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ClientUnsynchronized, OnChange = nameof(OnStationChanged))]
    public int RadioStationIndex { get; private set; } = -1;

    public LandVehicle Vehicle { get; set; } = null!;

    private GameObject? audioClientObject;
    private StreamAudioClient? audioClient;
    private AudioSource? audioSource;

    private void Awake()
    {
        if (AudioClientPrefab == null)
            throw new InvalidOperationException("AudioClientPrefab is null");
    }

    private void OnDestroy()
    {
        if (audioClientObject)
            Destroy(audioClientObject);

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
        RequestVehicleInfo(NetworkObject.LocalConnection);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void SetRadioStationIndex(int index)
    {
        if (index < -1 || index >= RadioStationManager.Instance.Stations.Count)
        {
            Plugin.Logger.LogWarning($"Invalid radio station index (out of range): {index}");
            return;
        }

        RadioStationIndex = index;
    }

    private void OnStationChanged(int prev, int next, bool asServer)
    {
        if (asServer)
            return;

        RadioStation? nextStation = next == -1 ? null : RadioStationManager.Instance.Stations.ElementAtOrDefault(next);

        Plugin.Logger.LogInfo($"New station: {nextStation?.Name} ({nextStation?.Url})");

        if (RadioStation != null)
            UnbindAudioClient();

        RadioStation = nextStation;

        if (RadioStation != null)
            InitAudioClient();
    }

    private void InitAudioClient()
    {
        if (RadioStation?.Url == null)
            return;

        if (audioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");

        if (HasOccupants())
            audioClientObject.SetActive(true);
        else
            audioClientObject.SetActive(false);

        if (audioClient == null)
        {
            audioClient = AudioStreamManager.Instance.GetOrCreateHost(RadioStation.Url).AddClient(audioClientObject);
            audioClient.ConvertToMono = true;
        }

        UpdateAudioEffects();
    }

    private void UnbindAudioClient()
    {
        if (RadioStation?.Url == null)
            throw new InvalidOperationException("Can not unbind. RadioStation or RadioStation.Url is null");

        if (audioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");

        if (audioClient != null)
        {
            AudioStreamManager.Instance.GetOrCreateHost(RadioStation.Url).DetachClient(audioClientObject);
            audioClient = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestVehicleInfo(NetworkConnection conn)
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
        if (!IsServer)
            return;

        Plugin.Logger.LogInfo($"Engine toggled: {started}");

        if (started && HasNPCOccupants())
        {
            RadioStationIndex = RadioStationManager.Instance.GetRandomNPCStationIndex();
        }

        InitAudioClient();
    }

    private void OnPlayerEnterVehicle(Player player)
    {
        if (player == null || !player.IsLocalPlayer)
            return;

        Plugin.Logger.LogInfo("Local player entered vehicle");
        UpdateAudioEffects();
    }

    private void OnPlayerExitVehicle(Player player)
    {
        if (player == null || !player.IsLocalPlayer)
            return;

        Plugin.Logger.LogInfo("Local player exited vehicle");
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
