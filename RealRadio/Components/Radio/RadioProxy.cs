using System;
using System.Collections;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RealRadio.Components.Audio;
using RealRadio.Data;
using UnityEngine;

namespace RealRadio.Components.Radio;

public abstract class RadioProxy : NetworkBehaviour
{
    [field: SerializeField]
    public GameObject AudioClientPrefab { get; private set; } = null!;

    public RadioStation? RadioStation { get; private set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ClientUnsynchronized, OnChange = nameof(OnStationChanged))]
    public int RadioStationIndex { get; private set; } = -1;

    protected GameObject? audioClientObject;
    protected StreamAudioClient? audioClient;
    protected AudioSource? audioSource;

    protected virtual void Awake()
    {
        if (AudioClientPrefab == null)
            throw new InvalidOperationException("AudioClientPrefab is null");
    }

    protected virtual void OnDestroy()
    {
        if (audioClientObject)
            Destroy(audioClientObject);
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

    private IEnumerator WaitForAudioClientThenEnable()
    {
        if (audioClientObject != null)
        {
            audioClientObject.SetActive(true);
            yield break;
        }

        yield return new WaitUntil(() => audioClientObject != null);
        audioClientObject!.SetActive(true);
    }

    protected virtual void OnStationChanged(int prev, int next, bool asServer)
    {
        if (asServer)
            return;

        RadioStation? nextStation = next == -1 ? null : RadioStationManager.Instance.Stations.ElementAtOrDefault(next);

        Plugin.Logger.LogInfo($"New station: {nextStation?.Name} ({nextStation?.Url})");

        if (RadioStation != null)
            UnbindAudioClient();

        RadioStation = nextStation;

        if (RadioStation != null)
        {
            InitAudioClient();
        }
    }

    protected virtual void InitAudioClient(bool delayStart = true)
    {
        if (RadioStation?.Url == null)
            return;

        if (audioClient)
        {
            Plugin.Logger.LogWarning("AudioClient is already running");
            return;
        }

        if (audioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");

        if (audioClient == null)
        {
            audioClient = AudioStreamManager.Instance.GetOrCreateHost(RadioStation.Url).AddClient(audioClientObject);
        }
    }

    protected virtual void UnbindAudioClient()
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
}
