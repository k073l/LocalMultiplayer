using System;
using System.Collections.Generic;
using System.Linq;
using RealRadio.Components.Building;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Yoga;

namespace RealRadio.Components.UI;

[RequireComponent(typeof(UIDocument))]
public class RadialMenu : Singleton<RadialMenu>
{
    public VisualTreeAsset RadialItemAsset = null!;

    public InteractableOption? HoveredOption { get; private set; }
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
    private UIDocument document = null!;
    private VisualElement root = null!;
    private Label optionLabel = null!;
    private VisualElement radialItemsContainer = null!;

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

            radialItemsContainer.Add(uiOption);
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
