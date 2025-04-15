using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace RealRadio.Components.Building.Buildables;

public class Radio : TogglableOffGridItem
{
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, WritePermissions = WritePermission.ServerOnly)]
    public int RadioStationIndex { get; [ServerRpc(RunLocally = true, RequireOwnership = false)] set; }

    private StreamAudioClient? audioClient;

    protected override void OnStateToggled(bool prev, bool next, bool asServer)
    {
        base.OnStateToggled(prev, next, asServer);

        if (asServer)
            return;

        if (next)
        {
            InitAndStartAudioClient();
        }
        else
        {
            if (audioClient != null)
                audioClient.enabled = false;
        }
    }

    private void InitAndStartAudioClient()
    {
        if (audioClient == null)
        {
            audioClient = AudioStreamManager.Instance.GetOrCreateHost("https://stream.simulatorradio.com").CreateClient(parent: transform);
            audioClient.ConvertToMono = true;
            return;
        }

        audioClient.enabled = true;
    }
}
