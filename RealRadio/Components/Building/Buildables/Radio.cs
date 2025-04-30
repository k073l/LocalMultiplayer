using System;
using System.Collections;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using RealRadio.Components.Audio;
using RealRadio.Components.Radio;
using RealRadio.Data;
using RealRadio.Events;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.Dialogue;
using ScheduleOne.Interaction;
using ScheduleOne.Management;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.UI.Compass;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class Radio : TogglableOffGridItem, IUsable
{
    public RadioStation? RadioStation { get; private set; }

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ClientUnsynchronized, OnChange = nameof(OnStationChanged))]
    public int RadioStationIndex { get; private set; }

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnVolumeChanged))]
    public float Volume { get; set; }

    public GameObject AudioClientObject = null!;

    public Transform ConfigureCameraTransform = null!;

    protected StreamAudioClient? audioClient;
    protected CrossfadeAudioSources crossFade = null!;

    private InteractableOptions? interactableOptions;
    private InteractableObject? interactableObject;

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    public NetworkObject? NPCUserObject { get; set; }

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnPlayerUserChanged))]
    public NetworkObject? PlayerUserObject { get; set; }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void SetRadioStationIndex(int index)
    {
        if (index < 0 || index >= RadioStationManager.Instance.Stations.Count)
        {
            Plugin.Logger.LogWarning($"Invalid radio station index (out of range): {index}");
            return;
        }

        RadioStationIndex = index;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void SetNPCUser(NetworkObject? npcObject)
    {
        NPCUserObject = npcObject;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void SetPlayerUser(NetworkObject? playerObject)
    {
        PlayerUserObject = playerObject;
    }

    protected virtual void OnPlayerUserChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
    }

    public override void Awake()
    {
        base.Awake();

        if (isGhost)
        {
            Destroy(AudioClientObject);
            return;
        }

        if (AudioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");

        crossFade = AudioClientObject.GetComponent<CrossfadeAudioSources>() ?? throw new InvalidOperationException("No CrossfadeAudioSources component found on AudioClientObject");

        if (ConfigureCameraTransform == null)
            throw new InvalidOperationException("ConfigureCameraTransform is null");

        interactableObject = GetComponentInChildren<InteractableObject>() ?? throw new InvalidOperationException("No InteractableObject component found in self or children");
        interactableOptions = GetComponentInChildren<InteractableOptions>() ?? throw new InvalidOperationException("No InteractableOptions component found in self or children");
        interactableOptions.OnInteract += OnInteract;
        interactableOptions.OnUpdateInteractionText += OnUpdateInteractionText;
    }

    public virtual void Update()
    {
        if (PlayerUserObject == Player.Local.NetworkObject)
        {
            if (GameInput.GetButtonDown(GameInput.ButtonCode.Escape))
            {
                StopConfiguring();
            }
        }
    }

    private void OnUpdateInteractionText(InteractableOption? option, EventRefData<string> data)
    {
        if (option?.Id == "toggle")
            data.Value = IsOn ? "Turn off" : "Turn on";
    }

    private void OnInteract(string optionId)
    {
        switch (optionId)
        {
            case "toggle":
                IsOn = !IsOn;
                break;
            case "configure":
                StartConfigureIfPossible();
                break;
            default:
                Plugin.Logger.LogWarning($"Unknown option id: {optionId}");
                break;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        RadioStationIndex = 0; // temp for testing
        OnStationChanged(-1, RadioStationIndex, true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnStationChanged(-1, RadioStationIndex, false);
    }

    protected override void OnStateToggled(bool prev, bool next, bool asServer)
    {
        base.OnStateToggled(prev, next, asServer);

        if (asServer || isGhost)
            return;

        if (next && RadioStation != null)
        {
            InitAudioClient();
        }
        else
        {
            if (audioClient != null)
                audioClient.gameObject.SetActive(false);
        }
    }

    protected virtual void OnStationChanged(int prev, int next, bool asServer)
    {
        var prevStation = prev == -1 ? null : RadioStationManager.Instance.Stations.ElementAtOrDefault(prev);
        var nextStation = next == -1 ? null : RadioStationManager.Instance.Stations.ElementAtOrDefault(next);

        if (nextStation == RadioStation)
            return;

        Plugin.Logger.LogInfo($"Radio station changing from {prevStation?.ToString() ?? "null"} to {nextStation?.ToString() ?? "null"}");

        if (RadioStation != null)
            UnbindAudioClient();

        RadioStation = nextStation;

        if (RadioStation != null)
        {
            InitAudioClient();
        }
    }

    protected virtual void OnVolumeChanged(float prev, float next, bool asServer)
    {
        if (asServer)
            return;

        Plugin.Logger.LogInfo($"Volume changing from {prev} to {next}");

        if (audioClient == null)
            return;

        crossFade.Volume = Mathf.Clamp01(next);
    }

    private void StartConfigureIfPossible()
    {
        if (((IUsable)this).IsInUse)
            return;

        Plugin.Logger.LogInfo("Start configure radio");
        SetPlayerUser(Player.Local.NetworkObject);
        OnStartConfigure();
    }

    private void StopConfiguring()
    {
        Plugin.Logger.LogInfo("Stop configure radio");
        OnEndConfigure();
        SetPlayerUser(null);
    }

    protected virtual void OnStartConfigure()
    {
        PlayerCamera.Instance.AddActiveUIElement(name);
        PlayerCamera.Instance.FreeMouse();
        PlayerCamera.Instance.OverrideTransform(ConfigureCameraTransform.position, ConfigureCameraTransform.rotation, lerpTime: 0.2f);
        PlayerCamera.Instance.OverrideFOV(60f, 0.2f);
        PlayerInventory.Instance.SetInventoryEnabled(false);
        PlayerMovement.Instance.canMove = false;
        CompassManager.Instance.SetVisible(false);
    }

    protected virtual void OnEndConfigure()
    {
        PlayerCamera.Instance.RemoveActiveUIElement(name);
        PlayerCamera.Instance.LockMouse();
        PlayerCamera.Instance.StopFOVOverride(0.2f);
        PlayerCamera.Instance.StopTransformOverride(0.2f);
        PlayerInventory.Instance.SetInventoryEnabled(true);
        PlayerMovement.Instance.canMove = true;
        CompassManager.Instance.SetVisible(true);
    }

    private void InitAudioClient()
    {
        if (RadioStation?.Url == null)
            throw new InvalidOperationException("Can not init. RadioStation or RadioStation.Url is null");

        if (IsOn)
            AudioClientObject.SetActive(true);
        else
            AudioClientObject.SetActive(false);

        if (audioClient == null)
        {
            audioClient = AudioStreamManager.Instance.GetOrCreateHost(RadioStation.Url).AddClient(AudioClientObject);
            audioClient.ConvertToMono = true;
            return;
        }
    }

    private void UnbindAudioClient()
    {
        if (RadioStation?.Url == null)
            throw new InvalidOperationException("Can not unbind. RadioStation or RadioStation.Url is null");

        if (audioClient != null)
        {
            AudioStreamManager.Instance.GetOrCreateHost(RadioStation.Url).DetachClient(AudioClientObject);
            audioClient = null;
        }
    }
}
