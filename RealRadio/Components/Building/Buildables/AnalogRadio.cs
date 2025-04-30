using System;
using FishNet.Object;
using RealRadio.Components.Radio;
using RealRadio.Components.UI.WorldUI;
using ScheduleOne;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealRadio.Components.Building.Buildables;

public class AnalogRadio : Radio
{
    [SerializeField] private DraggableSlider stationEditSlider = null!;

    public override void Awake()
    {
        base.Awake();

        if (stationEditSlider == null)
            throw new InvalidOperationException("StationEditSlider is null");

        stationEditSlider.LoopedAround.AddListener(OnSliderLoopedAround);
    }

    private void OnSliderLoopedAround(float direction)
    {
        int nextIndex = RadioStationIndex + (int)direction;
        nextIndex = (nextIndex + RadioStationManager.Instance.Stations.Count) % RadioStationManager.Instance.Stations.Count;

        if (nextIndex < 0)
            nextIndex += RadioStationManager.Instance.Stations.Count;

        SetRadioStationIndex(nextIndex);
    }

    protected override void OnPlayerUserChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
        base.OnPlayerUserChanged(prev, next, asServer);

        if (!asServer)
        {
            stationEditSlider.gameObject.SetActive(next == Player.Local.NetworkObject);
        }
    }
}
