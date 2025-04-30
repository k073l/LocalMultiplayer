using System;
using System.Runtime.CompilerServices;
using ScheduleOne;
using ScheduleOne.PlayerScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace RealRadio.Components.UI.WorldUI
{
    public class DraggableSlider : MonoBehaviour
    {
        public enum DragDirection
        {
            Horizontal,
            Vertical
        }

        public float Value
        {
            get => value;
            set
            {
                float newValue = Mathf.Clamp(value, MinValue, MaxValue);

                if (Mathf.Approximately(newValue, this.value))
                    return;

                this.value = newValue;
                ValueChanged?.Invoke(newValue);
            }
        }

        [Header("Drag Settings")]
        public float MaxValue = 100f;
        public float MinValue = 0f;
        [SerializeField] private float value;
#pragma warning disable CS0649 // Field is never assigned (fields are assigned by unity)
        [SerializeField] private DragDirection dragDirection = DragDirection.Horizontal;
        [SerializeField] private bool invertDragDirection;
        [SerializeField] private float dragMultiplier = 1f;
        [SerializeField] private bool loopAround;
#pragma warning restore CS0649

        [Header("Events")]
        public UnityEvent<float> ValueChanged = null!;
        public UnityEvent<float> LoopedAround = null!;

        [Header("References")]
        [SerializeField] private Collider dragCollider = null!;

        private Vector2? lockedMousePosition;
        private bool dragging;
        private bool cursorOverCollider;

        private void Awake()
        {
            if (dragCollider == null)
                throw new InvalidOperationException("DragCollider is null");
        }

        private void OnDisable()
        {
            EndInteraction();
        }

        private void EndInteraction()
        {
            lockedMousePosition = null;
            dragging = false;
            cursorOverCollider = false;
        }

        private void Update()
        {
            UpdateMouseRaycast();
            CheckInteractionStartInput();
            CheckInteractionEndInput();
            UpdateValueFromInput();
            UpdateCursor();
        }

        private void UpdateMouseRaycast()
        {
            cursorOverCollider = HoveringOverCollider();
        }

        private void CheckInteractionStartInput()
        {
            if (dragging)
                return;

            if (!GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
                return;

            if (!cursorOverCollider)
                return;

            dragging = true;
            lockedMousePosition = GameInput.MousePosition;
        }

        private void CheckInteractionEndInput()
        {
            if (!dragging)
                return;

            if (!GameInput.GetButtonUp(GameInput.ButtonCode.PrimaryClick))
                return;

            EndInteraction();
        }

        private void UpdateValueFromInput()
        {
            if (!dragging)
                return;

            float newValue;

            switch (dragDirection)
            {
                case DragDirection.Horizontal:
                    newValue = Value + GameInput.MouseDelta.x * dragMultiplier * (invertDragDirection ? -1f : 1f);
                    break;
                case DragDirection.Vertical:
                    newValue = Value + GameInput.MouseDelta.y * dragMultiplier * (invertDragDirection ? -1f : 1f);
                    break;
                default:
                    Plugin.Logger.LogWarning($"Unknown drag direction: {dragDirection}");
                    newValue = Value;
                    break;
            }

            if (!loopAround)
            {
                Value = newValue;
            }
            else
            {
                float range = MaxValue - MinValue;
                int eventCount = 0;
                float direction = 0;

                while (newValue > MaxValue)
                {
                    newValue -= range;
                    ++eventCount;
                    direction = 1f;
                }

                while (newValue < MinValue)
                {
                    newValue += range;
                    ++eventCount;
                    direction = -1f;
                }

                Value = newValue;

                for (int i = 0; i < eventCount; ++i)
                {
                    LoopedAround.Invoke(direction);
                }
            }
        }

        private void UpdateCursor()
        {
            CursorManager.ECursorType type = CursorManager.ECursorType.Default;

            if (cursorOverCollider)
                type = CursorManager.ECursorType.OpenHand;

            if (dragging)
                type = CursorManager.ECursorType.Grab;

            CursorManager.Instance.SetCursorAppearance(type);

            if (lockedMousePosition != null)
            {
                Mouse.current.WarpCursorPosition(lockedMousePosition.Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HoveringOverCollider()
        {
            if (!PlayerCamera.Instance.MouseRaycast(range: 3f, out var hit, Layers.UI.ToLayerMask()))
                return false;

            if (hit.collider != dragCollider)
                return false;

            return true;
        }
    }
}
