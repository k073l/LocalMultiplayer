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

    public override void Awake()
    {
        base.Awake();

        document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");
        root = document.rootVisualElement.Query(className: "root").First() ?? throw new InvalidOperationException("Could not find root ui element");
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
            Plugin.Logger.LogInfo($"Removed invisible class from {root}");
        }
        else
        {
            root.AddToClassList("invisible");
            Plugin.Logger.LogInfo($"Added invisible class to {root}");

            HoveredOption = null;
        }
    }

    private void RebuildMenu()
    {
    }
}
