using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RealRadio.Components.Building;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI;

[RequireComponent(typeof(UIDocument))]
public class RadialMenu : Singleton<RadialMenu>
{
    public VisualTreeAsset RadialItemAsset = null!;

    [field: SerializeField]
    public AudioSource? OptionChangeSound { get; set; }

    [field: SerializeField]
    public AudioSource? OptionSelectSound { get; set; }

    [field: SerializeField]
    public AudioSource? ShowSound { get; set; }

    [field: SerializeField]
    public AudioSource? HideSound { get; set; }

    public InteractableOption? HoveredOption
    {
        get => hoveredOption;
        private set
        {
            if (value == hoveredOption)
                return;


            if (hoveredOption != null)
            {
                var element = optionElements[hoveredOption];
                element.Query(name: "Root").First().RemoveFromClassList("hovered");
            }

            hoveredOption = value;

            if (hoveredOption != null)
            {
                var element = optionElements[hoveredOption];
                element.Query(name: "Root").First().AddToClassList("hovered");

                optionLabel.text = hoveredOption.Name;

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
    private InteractableOption? hoveredOption;
    private Vector2[] mouseDeltaHistory = new Vector2[10]; // 10 last averages of mouse delta
    private int mouseDeltaHistoryIndex;
    private float currentMouseHistoryTime;
    private int mouseDeltaSampleCount;

    public override void Awake()
    {
        base.Awake();

        if (RadialItemAsset == null)
            throw new InvalidOperationException("RadialItemDocument is not set");

        document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");
        root = document.rootVisualElement.Query(className: "root").First() ?? throw new InvalidOperationException("Could not find root ui element");
        optionLabel = root.Query<Label>(name: "SelectedOptionLabel").First() ?? throw new InvalidOperationException("Could not find option label ui element");
        radialItemsContainer = root.Query(name: "RadialItems").First() ?? throw new InvalidOperationException("Could not find radial items container ui element");
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

        if (currentMouseHistoryTime >= 0.05f)
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

    public void SetOptions(IEnumerable<InteractableOption> options)
    {
        this.options.Clear();
        this.options.AddRange(options);
        RebuildMenu();
    }

    public void Show(IEnumerable<InteractableOption> options, Action<InteractableOption>? onOptionSelected = null)
    {
        LockInput();
        SetOptions(options);
        IsVisible = true;
        RebuildMenu();
        OnOptionSelected += OnSelected;
        OnMenuOpened?.Invoke();

        void OnSelected(InteractableOption option)
        {
            OnOptionSelected -= OnSelected;
            onOptionSelected?.Invoke(option);
        }
    }

    public void Hide()
    {
        IsVisible = false;
        OnMenuClosed?.Invoke();
        UnlockInput();
    }

    private void OnVisibilityChanged()
    {
        if (isVisible)
        {
            root.RemoveFromClassList("invisible");
            PlayerCamera.Instance.AddActiveUIElement("RadialMenu");
        }
        else
        {
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
            root.style.backgroundImage = new StyleBackground(option.Sprite);

            radialItemsContainer.Add(uiOption);
            optionElements.Add(option, uiOption);
        }
    }

    private void PositionOptionElement(GeometryChangedEvent evt)
    {
        VisualElement uiOption = (VisualElement)evt.target;

        int i = (int)uiOption.userData;
        float sliceSize = 360 / options.Count;

        Vector2 middle = new Vector2(Screen.width / 2, Screen.height / 2);
        float angle = (sliceSize * i) - 90f;
        float offsetFromMiddle = GetItemOffsetFromMiddle();
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * offsetFromMiddle;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * offsetFromMiddle;

        uiOption.style.position = Position.Absolute;
        uiOption.style.left = new StyleLength(new Length(middle.x + x - (uiOption.resolvedStyle.width / 2), LengthUnit.Pixel));
        uiOption.style.top = new StyleLength(new Length(middle.y + y - (uiOption.resolvedStyle.height / 2), LengthUnit.Pixel));

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
}
