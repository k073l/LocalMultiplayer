using System;
using System.Collections;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RealRadio.Components.Audio;
using RealRadio.Components.Radio;
using RealRadio.Data;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class Radio : TogglableOffGridItem
{
    public RadioStation? RadioStation { get; private set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStationChanged))]
    public int RadioStationIndex { get; [ServerRpc(RequireOwnership = false)] set; } = -1;

    public GameObject AudioClientObject = null!;

    private StreamAudioClient? audioClient;

    public override void Awake()
    {
        base.Awake();

        if (isGhost)
            Destroy(AudioClientObject);
        else if (AudioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");
    }

    public override void Start()
    {
        base.Start();

        if (isGhost)
            return;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartCoroutine(SetRadioIndex());
    }

    private IEnumerator SetRadioIndex()
    {
        if (!NetworkManager.IsServer || RadioStationManager.Instance.Stations.Count == 0)
        {
            Plugin.Logger.LogInfo($"IsServer: {IsServer}, Stations.Count: {RadioStationManager.Instance.Stations.Count}");
            yield break;
        }

        while (!IsClientInitialized)
        {
            Plugin.Logger.LogInfo("Waiting for client initialization");
            yield return null;
        }

        RadioStationIndex = 0;
        OnStationChanged(-1, 0, true);
    }

    protected override void OnStateToggled(bool prev, bool next, bool asServer)
    {
        base.OnStateToggled(prev, next, asServer);

        Plugin.Logger.LogInfo($"OnStateToggled prev: {prev}, next: {next}, asServer: {asServer}");

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

        Plugin.Logger.LogInfo($"Radio station index changing from {prevStation?.ToString() ?? "null"} to {nextStation?.ToString() ?? "null"}");

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
