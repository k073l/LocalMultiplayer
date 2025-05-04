using System;
using FishNet.Object;
using RealRadio.Components.Radio;
using ScheduleOne.Map;

namespace RealRadio.Components.Buildings;

public class BuildingRadioProxy : RadioProxy
{
    private NPCEnterableBuilding? building;

    private void Start()
    {
        building = GetComponent<NPCEnterableBuilding>() ?? throw new InvalidOperationException("No NPCEnterableBuilding component found on game object");
    }
}
