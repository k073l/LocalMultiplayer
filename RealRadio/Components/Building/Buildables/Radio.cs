using System;
using System.Collections;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RealRadio.Components.Audio;
using RealRadio.Components.Radio;
using RealRadio.Data;
using RealRadio.Events;
using ScheduleOne.Interaction;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class Radio : TogglableOffGridItem
{
    public RadioStation? RadioStation { get; private set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ClientUnsynchronized, OnChange = nameof(OnStationChanged))]
    public int RadioStationIndex { get; private set; }

    public GameObject AudioClientObject = null!;

    private StreamAudioClient? audioClient;

    private InteractableOptions? interactableOptions;
    private InteractableObject? interactableObject;

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

        interactableObject = GetComponentInChildren<InteractableObject>() ?? throw new InvalidOperationException("No InteractableObject component found in self or children");
        interactableOptions = GetComponentInChildren<InteractableOptions>() ?? throw new InvalidOperationException("No InteractableOptions component found in self or children");
        interactableOptions.OnInteract += OnInteract;
        interactableOptions.OnUpdateInteractionText += OnUpdateInteractionText;
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
                Plugin.Logger.LogInfo("Start configure radio");
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

    private void OnStationChanged(int prev, int next, bool asServer)
    {
        Plugin.Logger.LogInfo($"prev: {prev}, next: {next}, asServer: {asServer}");

        var prevStation = prev == -1 ? null : RadioStationManager.Instance.Stations.ElementAtOrDefault(prev);
        var nextStation = next == -1 ? null : RadioStationManager.Instance.Stations.ElementAtOrDefault(next);

        if (nextStation == RadioStation)
            return;

        Plugin.Logger.LogDebug($"Radio station changing from {prevStation?.ToString() ?? "null"} to {nextStation?.ToString() ?? "null"}");

        if (RadioStation != null)
            UnbindAudioClient();

        RadioStation = nextStation;

        if (RadioStation != null)
        {
            InitAudioClient();
        }
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
