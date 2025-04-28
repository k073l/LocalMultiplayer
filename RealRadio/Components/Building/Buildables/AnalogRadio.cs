using System;
using RealRadio.Components.Radio;
using ScheduleOne;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class AnalogRadio : Radio
{
    public float DragDistanceToChangeStation = 1f;

    public bool StationHorizontalMouseDragDirection;
    public bool StationInvertDragDirection;

    [SerializeField] private Collider stationEditCollider = null!;

    private bool editingStation;
    private float stationDragDistance;

    public override void Awake()
    {
        base.Awake();

        if (stationEditCollider == null)
            throw new InvalidOperationException("StationEditCollider is null");
    }

    public override void Update()
    {
        base.Update();

        if (PlayerUserObject != Player.Local.NetworkObject)
            return;

        CheckStationInteractionStart();
        CheckStationInteractionEnd();
        UpdateStationIndexFromInput();
        UpdateCursor();
    }

    private void CheckStationInteractionStart()
    {
        if (editingStation)
            return;

        if (!GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
            return;

        if (!HoveringOverStationEditCollider())
            return;

        editingStation = true;
    }

    private bool HoveringOverStationEditCollider()
    {
        if (!PlayerCamera.Instance.MouseRaycast(range: 10f, out var hit, Layers.UI.ToLayerMask()))
            return false;

        if (hit.collider != stationEditCollider)
            return false;

        return true;
    }

    private void CheckStationInteractionEnd()
    {
        if (!editingStation)
            return;

        if (!GameInput.GetButtonUp(GameInput.ButtonCode.PrimaryClick))
            return;

        editingStation = false;
        stationDragDistance = 0f;
    }

    private void UpdateStationIndexFromInput()
    {
        if (!editingStation)
            return;

        if (StationHorizontalMouseDragDirection)
            stationDragDistance += GameInput.MouseDelta.x * (StationInvertDragDirection ? -1f : 1f);
        else
            stationDragDistance += GameInput.MouseDelta.y * (StationInvertDragDirection ? -1f : 1f);

        if (Math.Abs(stationDragDistance) >= DragDistanceToChangeStation)
        {
            int direction = Math.Sign(stationDragDistance);
            stationDragDistance = 0f;

            int newIndex = (RadioStationIndex + direction) % RadioStationManager.Instance.Stations.Count;

            if (newIndex < 0)
                newIndex += RadioStationManager.Instance.Stations.Count;

            SetRadioStationIndex(newIndex);
        }
    }

    private void UpdateCursor()
    {
        CursorManager.ECursorType type = CursorManager.ECursorType.Default;

        if (HoveringOverStationEditCollider())
            type = CursorManager.ECursorType.OpenHand;

        if (editingStation)
            type = CursorManager.ECursorType.Grab;

        CursorManager.Instance.SetCursorAppearance(type);
    }
}
