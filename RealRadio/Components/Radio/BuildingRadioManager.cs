using System;
using System.Collections.Generic;
using System.Linq;
using RealRadio.Components.Buildings;
using ScheduleOne.DevUtilities;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Schedules;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class BuildingRadioManager : NetworkSingleton<BuildingRadioManager>
{
    [field: SerializeField]
    public GameObject RadioProxyPrefab { get; private set; } = null!;

    public Dictionary<int, NPCEnterableBuilding> Buildings { get; private set; } = null!;
    public Dictionary<NPCEnterableBuilding, BuildingRadioProxy> Proxies { get; } = [];
    public Dictionary<NPCEnterableBuilding, HashSet<NPC>> Residents { get; } = [];

    private static readonly string[] blackListedBuildingWords =
    [
        "pizzeria",
        "office",
        "pharmacy",
        "store",
        "warehouse",
        "chemical",
        "industrial",
        "center",
        "cafe",
        "diner",
        "bar",
        "bank",
        "chapel",
        "tent",
        "hut",
        "arcade",
        "restaurant",
        "shooting",
        "barber",
        "nightclub",
        "station",
        "market",
        "container",
        "caravan",
        "courthouse",
        "townhall",
    ];

    public override void Awake()
    {
        base.Awake();

        if (RadioProxyPrefab == null)
            throw new InvalidOperationException("RadioProxyPrefab is null");
    }

    public static int GetBuildingHash(NPCEnterableBuilding building)
    {
        int hash;

        unchecked
        {
            hash = building.GUID.GetHashCode() * 31 * building.BuildingName.GetHashCode();
        }

        Plugin.Logger.LogInfo($"Building hash: {building.GUID} - {building.BuildingName} ({hash})");

        return hash;
    }

    private void InitResidents()
    {
        var npcs = FindObjectsOfType<NPC>(includeInactive: true);
        var buildings = new List<NPCEnterableBuilding>(capacity: 1);

        foreach (var npc in npcs)
        {
            buildings.Clear();
            var schedule = npc.GetComponentInChildren<NPCScheduleManager>();

            if (schedule == null)
                continue;

            foreach (var action in schedule.ActionList)
            {
                if (action is not NPCEvent_StayInBuilding stayInBuilding)
                    continue;

                var building = stayInBuilding.Building;

                // Skip non residential buildings
                if (!BuildingIsResidential(building))
                    continue;

                buildings.Add(building);
            }

            foreach (var building in buildings)
            {
                if (!Residents.TryGetValue(building, out var residents))
                {
                    residents = new();
                    Residents.Add(building, residents);
                }

                residents.Add(npc);
            }
        }
    }

    private void InitBuildings()
    {
        var buildings = FindObjectsOfType<NPCEnterableBuilding>();
        Buildings = new(capacity: buildings.Length);

        foreach (var building in buildings)
        {
            // Skip non residential buildings
            if (!BuildingIsResidential(building))
                continue;

            // Mick's house seems to have double components added for some reason, so we need to check if the building already exists
            if (!Buildings.TryAdd(GetBuildingHash(building), building))
            {
                Plugin.Logger.LogWarning($"Found duplicate building: {building.GUID} - {building.BuildingName}");
            }
        }
    }

    private bool BuildingIsResidential(NPCEnterableBuilding building)
    {
        if (building is MedicalCentre)
            return false;

        if (building is PoliceStation)
            return false;

        var name = building.name.ToLowerInvariant();
        var buildingName = building.BuildingName.ToLowerInvariant();

        foreach (var word in blackListedBuildingWords)
        {
            if (name.Contains(word) || buildingName.Contains(word))
                return false;
        }

        return true;
    }

    public void AddProxy(BuildingRadioProxy proxy)
    {
        if (proxy.Building == null)
            throw new InvalidOperationException("Building proxy is null");

        if (!Proxies.TryAdd(proxy.Building, proxy))
        {
            Plugin.Logger.LogWarning($"Found duplicate proxy: {proxy.Building.GUID} - {proxy.Building.BuildingName}");
        }
    }

    public void RemoveProxy(BuildingRadioProxy proxy)
    {
        if (proxy.Building == null)
            throw new InvalidOperationException("Building proxy is null");

        if (!Proxies.Remove(proxy.Building))
        {
            Plugin.Logger.LogWarning($"Tried to remove unknown building proxy: {proxy.Building.GUID} - {proxy.Building.BuildingName}");
        }
    }

    public override void Start()
    {
        base.Start();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsClientOnly)
        {
            InitBuildings();
            InitResidents();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        InitBuildings();
        InitResidents();

        foreach (var building in Buildings.Values)
        {
            var proxy = Instantiate(RadioProxyPrefab, parent: transform);
            proxy.GetComponent<BuildingRadioProxy>().Building = building;

            Spawn(proxy);
        }
    }
}
