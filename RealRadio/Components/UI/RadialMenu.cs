using System;
using System.Collections.Generic;
using RealRadio.Components.Building;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI;

[RequireComponent(typeof(UIDocument))]
public class RadialMenu : Singleton<RadialMenu>
{
    public VisualTreeAsset RadialItemAsset = null!;

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
            }
            else
            {
                optionLabel.text = string.Empty;
            }
        }
    }
    public Action<InteractableOption>? OnOptionSelected { get; set; }

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

        OnVisibilityChanged();
    }

    private void Update()
    {
        if (!isVisible)
            return;

        UpdateHoveredOption();
    }

    private void UpdateHoveredOption()
    {
        float sliceSize = 360 / options.Count;
        float angle = Mathf.Atan2(Input.mousePosition.y - Screen.height / 2, Input.mousePosition.x - Screen.width / 2) * Mathf.Rad2Deg;
        angle -= 90f;

        angle = angle < 0 ? angle + 360f : angle;
        angle = 360f - angle;

        // Round index to nearest slice
        int index = Mathf.RoundToInt(angle / sliceSize);

        if (index == options.Count)
            index = 0;

        HoveredOption = options[index];
    }

    public void SetOptions(IEnumerable<InteractableOption> options)
    {
        this.options.Clear();
        this.options.AddRange(options);
        RebuildMenu();
    }

    public void Show(IEnumerable<InteractableOption> options)
    {
        SetOptions(options);
        IsVisible = true;
        RebuildMenu();
    }

    public void Hide()
    {
        IsVisible = false;
    }

    private void OnVisibilityChanged()
    {
        if (isVisible)
        {
            root.RemoveFromClassList("invisible");
        }
        else
        {
            root.AddToClassList("invisible");
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
        float offsetFromMiddle = Mathf.Min(Screen.width, Screen.height) * 0.2f;
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * offsetFromMiddle;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * offsetFromMiddle;

        uiOption.style.position = Position.Absolute;
        uiOption.style.left = new StyleLength(new Length(middle.x + x - (uiOption.resolvedStyle.width / 2), LengthUnit.Pixel));
        uiOption.style.top = new StyleLength(new Length(middle.y + y - (uiOption.resolvedStyle.height / 2), LengthUnit.Pixel));

        uiOption.MarkDirtyRepaint();
        uiOption.visible = true;
    }
}
