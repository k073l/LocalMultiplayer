using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class Radio : TogglableOffGridItem
{
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, WritePermissions = WritePermission.ServerOnly)]
    public int RadioStationIndex { get; [ServerRpc(RunLocally = true, RequireOwnership = false)] set; }

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

    protected override void OnStateToggled(bool prev, bool next, bool asServer)
    {
        base.OnStateToggled(prev, next, asServer);

        if (asServer || isGhost)
            return;

        if (next)
        {
            InitAndStartAudioClient();
        }
        else
        {
            if (audioClient != null)
                audioClient.gameObject.SetActive(false);
        }
    }

    private void InitAndStartAudioClient()
    {
        if (audioClient == null)
        {
            audioClient = AudioStreamManager.Instance.GetOrCreateHost("https://stream.simulatorradio.com").AddClient(AudioClientObject);
            audioClient.ConvertToMono = true;
            return;
        }

        audioClient.gameObject.SetActive(true);
    }
}
