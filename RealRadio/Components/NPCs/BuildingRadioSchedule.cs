using System;
using System.Net;
using RealRadio.Components.Buildings;
using RealRadio.Components.Radio;
using ScheduleOne.GameTime;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.NPCs.Schedules;
using UnityEngine;
using Behaviour = ScheduleOne.NPCs.Behaviour.Behaviour;

namespace RealRadio.Components.NPCs;

[RequireComponent(typeof(NPC))]
public class BuildingRadioSchedule : MonoBehaviour
{
    private NPC npc = null!;
    private NPCScheduleManager schedule = null!;

    private NPCAction? lastAction;
    private Behaviour? lastBehaviour;
    private Behaviour? lastNonScheduleBehaviour;
    private NPCEnterableBuilding? lastBuilding => npc.CurrentBuilding ?? npc.LastEnteredDoor?.Building;
    private bool enteringBuilding;
    private bool summoned;

    private void Awake()
    {
        npc = GetComponent<NPC>() ?? throw new InvalidOperationException("Could not find NPC component");
        schedule = npc.GetComponentInChildren<NPCScheduleManager>() ?? throw new InvalidOperationException("Could not find NPCScheduleManager component");
    }

    private void LateUpdate()
    {
        UpdateNpcBehaviour();
    }

    private void UpdateNpcBehaviour()
    {
        if (!schedule.isActiveAndEnabled)
            return;

        bool actionChanged = false, behaviourChanged = false;
        NPCAction? action = schedule.ActiveAction;
        Behaviour? behaviour = npc.behaviour.activeBehaviour;

        if (action != lastAction)
            actionChanged = true;

        if (behaviour != lastBehaviour)
            behaviourChanged = true;

        if (actionChanged || behaviourChanged)
        {
            OnBehaviourChanged(behaviour);
        }

        if (enteringBuilding && action is NPCEvent_StayInBuilding stayInBuilding)
        {
            if (npc.CurrentBuilding == stayInBuilding.Building)
            {
                enteringBuilding = false;

                bool returningFromSummon = lastNonScheduleBehaviour?.name.Contains("summon", StringComparison.OrdinalIgnoreCase) ?? false;
                OnEnterBuilding(stayInBuilding.Building, returningFromSummon);
            }
        }
        else
        {
            enteringBuilding = false;
        }

        if (actionChanged)
        {
            lastAction = action;
        }

        if (behaviourChanged)
        {
            lastBehaviour = behaviour;

            if (behaviour is not ScheduleBehaviour)
                lastNonScheduleBehaviour = behaviour;
        }
    }

    private void OnBehaviourChanged(Behaviour? behaviour)
    {
        if (behaviour is ScheduleBehaviour scheduleBehaviour)
        {
            var activeAction = scheduleBehaviour.schedule.ActiveAction;

            if (activeAction is NPCEvent_StayInBuilding stayInBuilding && npc.CurrentBuilding != stayInBuilding.Building)
            {
                enteringBuilding = true;
            }
            else if (lastAction is NPCEvent_StayInBuilding lastStayInBuilding)
            {
                OnExitBuilding(lastStayInBuilding.Building, behaviour?.name.Contains("summon", StringComparison.OrdinalIgnoreCase) ?? false);
            }
        }
        else
        {
            if (lastBuilding != null && !summoned)
            {
                OnExitBuilding(lastBuilding, behaviour?.name.Contains("summon", StringComparison.OrdinalIgnoreCase) ?? false);
            }
        }
    }

    private void OnEnterBuilding(NPCEnterableBuilding building, bool returningFromSummon)
    {
        summoned = false;

        if (building.OccupantCount == 1)
        {
            var proxy = BuildingRadioManager.Instance.GetProxy(building);

            if (proxy == null)
                return;

            if ((!returningFromSummon || proxy.RadioStationIndex == -1) && proxy.StartTime <= TimeManager.Instance.DailyMinTotal && proxy.StopTime > TimeManager.Instance.DailyMinTotal && UnityEngine.Random.Range(0f, 1f) <= 0.5f)
                proxy.SetRadioStationIndex(RadioStationManager.Instance.GetRandomNPCStationIndex());
        }
    }

    private void OnExitBuilding(NPCEnterableBuilding building, bool summoned)
    {
        this.summoned = summoned;

        if (building.OccupantCount == 0)
        {
            var proxy = BuildingRadioManager.Instance.GetProxy(building);

            if (proxy == null)
                return;

            if (!summoned)
                proxy.SetRadioStationIndex(-1);
        }
    }
}
