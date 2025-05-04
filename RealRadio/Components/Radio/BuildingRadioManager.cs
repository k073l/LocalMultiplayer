using System;
using System.Collections.Generic;
using System.Linq;
using RealRadio.Components.Buildings;
using ScheduleOne.DevUtilities;
using ScheduleOne.Map;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class BuildingRadioManager : NetworkSingleton<BuildingRadioManager>
{
    [field: SerializeField]
    public GameObject RadioProxyPrefab { get; private set; } = null!;

    public Dictionary<int, NPCEnterableBuilding> Buildings { get; private set; } = null!;

    public override void Awake()
    {
        base.Awake();

        if (RadioProxyPrefab == null)
            throw new InvalidOperationException("RadioProxyPrefab is null");

        var buildings = FindObjectsOfType<NPCEnterableBuilding>();
        Buildings = new(capacity: buildings.Length);

        foreach (var building in buildings)
        {
            // Mick's house seems to have double components added for some reason, so we need to check if the building already exists
            if (!Buildings.TryAdd(GetBuildingHash(building), building))
            {
                Plugin.Logger.LogWarning($"Building already exists: {building.GUID} - {building.BuildingName}");
            }
        }
    }

    public static int GetBuildingHash(NPCEnterableBuilding building)
    {
        unchecked
        {
            return building.GUID.GetHashCode() * 31 * building.BuildingName.GetHashCode();
        }
    }

    public override void Start()
    {
        base.Start();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        foreach (var building in Buildings.Values)
        {
            var proxy = Instantiate(RadioProxyPrefab, parent: transform);
            proxy.name = $"{proxy.name} - {building.name}";
            proxy.GetComponent<BuildingRadioProxy>().Building = building;

            Spawn(proxy);
        }
    }
}
