using System.Collections.Generic;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class RadioStationManager : PersistentSingleton<RadioStationManager>
{
    [SerializeField]
    private RadioStation[] defaultStations = [];
}
