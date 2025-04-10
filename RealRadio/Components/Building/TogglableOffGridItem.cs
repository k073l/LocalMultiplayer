using FishNet.Object.Synchronizing;
using UnityEngine;

namespace RealRadio.Components.Building;

public class TogglableOffGridItem : OffGridItem
{
    [Header("Togglable")]
    [SyncVar]
    public bool IsOn;
}
