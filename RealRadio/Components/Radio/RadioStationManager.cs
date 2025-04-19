using System;
using System.Collections.Generic;
using System.Linq;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class RadioStationManager : PersistentSingleton<RadioStationManager>
{
    public Action? OnStationsChanged;
    public IReadOnlyList<RadioStation> Stations => stations;
    public IReadOnlyList<RadioStation> SortedStations { get; private set; } = null!;

    private List<RadioStation> stations = [];
    private bool stationsChanged;

    public override void Awake()
    {
        base.Awake();

        if (Plugin.Assets?.DefaultRadioStations != null)
        {
            stations.AddRange(Plugin.Assets.DefaultRadioStations);
            InternalOnStationsChanged();
        }
    }

    public void AddRadioStation(RadioStation station)
    {
        stations.Add(station);
        stationsChanged = true;
    }

    public void RemoveRadioStation(RadioStation station)
    {
        stations.Remove(station);
        stationsChanged = true;
    }

    private void LateUpdate()
    {
        if (stationsChanged)
        {
            stationsChanged = false;
            InternalOnStationsChanged();
        }
    }

    private void InternalOnStationsChanged()
    {
        UpdateSortedStations();
        OnStationsChanged?.Invoke();
    }

    private void UpdateSortedStations()
    {
        SortedStations = [.. Stations.OrderBy(x => x.Name)];
    }
}
