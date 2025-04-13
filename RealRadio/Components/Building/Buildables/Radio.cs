using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace RealRadio.Components.Building.Buildables;

public class Radio : TogglableOffGridItem
{
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, WritePermissions = WritePermission.ServerOnly)]
    public int RadioStationIndex { get; [ServerRpc(RunLocally = true, RequireOwnership = false)] set; }
}
