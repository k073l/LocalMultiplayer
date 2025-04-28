using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RealRadio.Components.Building;
using RealRadio.Events;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI;

[RequireComponent(typeof(UIDocument))]
public class RadialMenu : Singleton<RadialMenu>
{
    public VisualTreeAsset RadialItemAsset = null!;

    [field: SerializeField]
    public AudioSource? OptionChangeSound { get; private set; }

    [field: SerializeField]
    public AudioSource? OptionSelectSound { get; private set; }

    [field: SerializeField]
    public AudioSource? ShowSound { get; private set; }

    [field: SerializeField]
    public AudioSource? HideSound { get; private set; }

    public InteractableOption? HoveredOption
    {
        get => hoveredOption;
        private set
        {
            if (value == hoveredOption)
                return;

            DisableHoverEffectsOnActiveOption();

            hoveredOption = value;
            arrowOffset = null;

            if (hoveredOption != null)
            {
                EnableHoverEffectsOnHoveredOption();

                var eventData = new EventRefData<string>(hoveredOption.name);
                OnUpdateInteractionText?.Invoke(hoveredOption, eventData);
                optionLabel.text = eventData.Value;

                OnHoveredOptionChanged?.Invoke(hoveredOption);
            }
            else
            {
                optionLabel.text = string.Empty;
            }
        }
    }

    public Action<InteractableOption>? OnOptionSelected { get; set; }
    public Action<InteractableOption>? OnHoveredOptionChanged { get; set; }
    public Action<InteractableOption, EventRefData<string>>? OnUpdateInteractionText { get; set; }
    public Action? OnMenuOpened { get; set; }
    public Action? OnMenuClosed { get; set; }

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            if (value == isVisible)
                return;

            isVisible = value;
            OnVisibilityChanged();
        }
    }

    private bool isVisible;
    private List<InteractableOption> options = new(capacity: 16);
    private Dictionary<InteractableOption, VisualElement> optionElements = new(capacity: 16);
    private UIDocument document = null!;
    private VisualElement root = null!;
    private Label optionLabel = null!;
    private VisualElement radialItemsContainer = null!;
    private VisualElement arrow = null!;
    private float? arrowOffset;
    private InteractableOption? hoveredOption;
    private Vector2[] mouseDeltaHistory = new Vector2[20];
    private int mouseDeltaHistoryIndex;
    private float currentMouseHistoryTime;
    private int mouseDeltaSampleCount;

    public override void Awake()
    {
        base.Awake();

        if (RadialItemAsset == null)
            throw new InvalidOperationException("RadialItemAsset is not set");

        document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");
        root = document.rootVisualElement.Query(className: "root").First() ?? throw new InvalidOperationException("Could not find root ui element");
        optionLabel = root.Query<Label>(name: "SelectedOptionLabel").First() ?? throw new InvalidOperationException("Could not find option label ui element");
        radialItemsContainer = root.Query(name: "RadialItems").First() ?? throw new InvalidOperationException("Could not find radial items container ui element");
        arrow = root.Query(name: "Arrow").First() ?? throw new InvalidOperationException("Could not find arrow ui element");
    }

    public override void Start()
    {
        base.Start();

        OnMenuOpened += () => ShowSound?.Play();
        OnMenuClosed += () => HideSound?.Play();
        OnHoveredOptionChanged += (option) => OptionChangeSound?.Play();
        OnOptionSelected += (option) => OptionSelectSound?.Play();
        OnVisibilityChanged();
    }

    private void Update()
    {
        if (!isVisible)
            return;

        UpdateAverageMouseDelta();
        UpdateArrow();
        UpdateHoveredOption();
        CheckForPrimaryInput();
    }

    private void UpdateHoveredOption()
    {
        float sliceSize = 360 / options.Count;
        float angle = GetMouseAngleFromMiddle();

        // Round index to nearest slice
        int index = Mathf.RoundToInt(angle / sliceSize);

        if (index == options.Count)
            index = 0;

        HoveredOption = options[index];
    }

    private void CheckForPrimaryInput()
    {
        if (!GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
            return;

        if (HoveredOption == null)
        {
            Plugin.Logger.LogWarning("Hovered option is null");
            return;
        }

        OnOptionSelected?.Invoke(HoveredOption);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetMouseAngleFromMiddle()
    {
        Vector2 mouseDelta = GetAverageMouseDelta();
        float angle = Mathf.Atan2(mouseDelta.y, mouseDelta.x) * Mathf.Rad2Deg;
        angle -= 90f;

        angle = angle < 0 ? angle + 360f : angle;
        angle = 360f - angle;
        return angle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateAverageMouseDelta()
    {
        currentMouseHistoryTime += Time.unscaledDeltaTime;
        Vector2 mouseDelta = GameInput.MouseDelta;

        if (mouseDelta.sqrMagnitude > 0)
        {
            mouseDeltaHistory[mouseDeltaHistoryIndex] += mouseDelta;
            ++mouseDeltaSampleCount;
        }

        if (currentMouseHistoryTime >= 0.01f)
        {
            mouseDeltaHistoryIndex = (mouseDeltaHistoryIndex + 1) % mouseDeltaHistory.Length;
            currentMouseHistoryTime = 0f;

            if (mouseDeltaSampleCount > 0)
            {
                mouseDeltaHistory[mouseDeltaHistoryIndex] /= mouseDeltaSampleCount;
                mouseDeltaSampleCount = 0;
            }
        }
    }

    private Vector2 GetAverageMouseDelta()
    {
        Vector2 average = Vector2.zero;

        for (int i = 0; i < mouseDeltaHistory.Length; ++i)
        {
            average += mouseDeltaHistory[i];
        }

        return average / mouseDeltaHistory.Length;
    }

    private void ResetMouseDeltaHistory()
    {
        Array.Fill(mouseDeltaHistory, new Vector2(0, 1f));
        mouseDeltaHistoryIndex = 0;
        currentMouseHistoryTime = 0f;
        mouseDeltaSampleCount = 0;
    }

    private void UpdateArrow()
    {
        float angle = GetMouseAngleFromMiddle();
        arrow.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(angle, AngleUnit.Degree)));

        arrow.style.position = Position.Absolute;
        arrow.style.left = (Screen.width / 2) - (arrow.resolvedStyle.width / 2);
        arrow.style.top = (Screen.height / 2) - (arrow.resolvedStyle.height / 2);

        if (arrowOffset == null && HoveredOption != null && optionElements.TryGetValue(HoveredOption, out var hoveredElement))
        {
            var size = hoveredElement.resolvedStyle.width;

            if (size != float.NaN)
                arrowOffset = GetItemOffsetFromMiddle() - size;
        }

        if (arrowOffset != null)
        {
            var root = arrow.Query(name: "Root").First();
            var translation = root.style.translate.value;
            translation.x = new Length(-50f, Length.Unit.Percent);
            translation.y = -arrowOffset.Value;
            root.style.translate = translation;
        }
    }

    public void SetOptions(IEnumerable<InteractableOption> options)
    {
        this.options.Clear();
        this.options.AddRange(options);
        RebuildMenu();
    }

    /// <summary>
    /// Shows the radial menu with the given options. If the menu is already open it will not be updated with the new options and the callback will never be called.
    /// </summary>
    /// <param name="onOptionSelected">This callback will be invoked when an option is selected. It will only be called once for the current menu.</param>
    /// <param name="onUpdateInteractionText">This callback will be invoked when updating the interaction text. It will only be called while the current menu is open.</param>
    /// <returns>True if the menu was shown, false if it's already open.</returns>
    public bool Show(
        IEnumerable<InteractableOption> options,
        Action<InteractableOption>? onOptionSelected = null,
        Action<InteractableOption, EventRefData<string>>? onUpdateInteractionText = null
    )
    {
        if (IsVisible)
            return false;

        IsVisible = true;
        ResetMouseDeltaHistory();
        LockInput();
        SetOptions(options);
        RebuildMenu();
        OnOptionSelected += OnSelected;
        OnUpdateInteractionText += onUpdateInteractionText;
        OnMenuClosed += MenuClosed;
        OnMenuOpened?.Invoke();

        return true;

        void MenuClosed()
        {
            OnMenuClosed -= MenuClosed;
            OnOptionSelected -= OnSelected;
            OnUpdateInteractionText -= UpdateInteractionText;
        }

        void OnSelected(InteractableOption option)
        {
            onOptionSelected?.Invoke(option);
        }

        void UpdateInteractionText(InteractableOption option, EventRefData<string> data)
        {
            onUpdateInteractionText?.Invoke(option, data);
        }
    }

    public void Hide()
    {
        if (!IsVisible)
            return;

        IsVisible = false;
        OnMenuClosed?.Invoke();
        UnlockInput();
    }

    private void OnVisibilityChanged()
    {
        if (isVisible)
        {
            root.style.display = DisplayStyle.Flex;
            root.RemoveFromClassList("invisible");
            PlayerCamera.Instance.AddActiveUIElement("RadialMenu");
        }
        else
        {
            root.style.display = DisplayStyle.None;
            root.AddToClassList("invisible");
            PlayerCamera.Instance.RemoveActiveUIElement("RadialMenu");
            HoveredOption = null;
        }
    }

    private void RebuildMenu()
    {
        // Remove existing children
        radialItemsContainer.Clear();

        // Clear mapping of option elements
        optionElements.Clear();

        if (options.Count == 0)
        {
            Plugin.Logger.LogWarning("No options to display in radial menu");
            return;
        }

        for (int i = 0; i < options.Count; ++i)
        {
            InteractableOption option = options[i];
            VisualElement uiOption = RadialItemAsset.Instantiate();
            uiOption.visible = false;
            uiOption.userData = i;
            uiOption.RegisterCallback<GeometryChangedEvent>(PositionOptionElement);

            var root = uiOption.Query(name: "Root").First();

            if (option.Sprite != null)
                root.style.backgroundImage = new StyleBackground(option.Sprite);

            radialItemsContainer.Add(uiOption);
            optionElements.Add(option, uiOption);
        }
    }

    private void PositionOptionElement(GeometryChangedEvent evt)
    {
        VisualElement uiOption = (VisualElement)evt.target;

        if (uiOption.resolvedStyle.width == float.NaN)
            return;

        uiOption.style.position = Position.Absolute;
        uiOption.style.left = (Screen.width / 2) - (uiOption.resolvedStyle.width / 2);
        uiOption.style.top = (Screen.height / 2) - (uiOption.resolvedStyle.height / 2);

        int i = (int)uiOption.userData;
        float sliceSize = 360 / options.Count;

        float angle = (sliceSize * i) - 90f;
        float offsetFromMiddle = GetItemOffsetFromMiddle();
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * offsetFromMiddle;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * offsetFromMiddle;

        var translation = new Translate(
            new Length(x, LengthUnit.Pixel),
            new Length(y, LengthUnit.Pixel)
        );
        var root = uiOption.Query(name: "Root").First();
        root.style.translate = translation;

        uiOption.MarkDirtyRepaint();
        uiOption.visible = true;
    }

    private static float GetItemOffsetFromMiddle()
    {
        return Mathf.Min(Screen.width, Screen.height) * 0.2f;
    }

    private void LockInput()
    {
        PlayerCamera.Instance.SetCanLook(false);
        HUD.Instance.SetCrosshairVisible(false);
    }

    private void UnlockInput()
    {
        PlayerCamera.Instance.SetCanLook(true);
        HUD.Instance.SetCrosshairVisible(true);
    }

    private void DisableHoverEffectsOnActiveOption()
    {
        if (hoveredOption != null)
        {
            var element = optionElements[hoveredOption];
            element.Query(name: "Root").First().RemoveFromClassList("hovered");
        }
    }

    private void EnableHoverEffectsOnHoveredOption()
    {
        if (hoveredOption == null)
            return;

        var element = optionElements[hoveredOption];
        element.Query(name: "Root").First().AddToClassList("hovered");
    }
}
