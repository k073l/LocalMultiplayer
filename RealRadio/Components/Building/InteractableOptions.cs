using System;
using System.Collections.Generic;
using System.Linq;
using RealRadio.Components.UI;
using RealRadio.Events;
using ScheduleOne.Interaction;
using ScheduleOne.Management.Presets.Options;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.UI.Shop;
using UnityEngine;
using UnityEngine.Events;

namespace RealRadio.Components.Building;

public class InteractableOptions : MonoBehaviour
{
    public const float MaxHoldTimeBeforeShowOptions = 0.2f;

    /// <summary>
    /// The interactable object to listen for interactions on.
    /// If it's null the first interactable object found in self or children will be used.
    /// </summary>
    public InteractableObject InteractableObject = null!;

    /// <summary>
    /// Called when the interactable object is interacted with.
    /// The string is the id of the option that was selected.
    /// </summary>
    public UnityAction<string>? OnInteract;

    /// <summary>
    /// Called when the interactable object is interacted with.
    ///
    /// The option is the default option (if any), and the event ref data is the interaction text to show.
    /// </summary>
    public Action<InteractableOption?, EventRefData<string>>? OnUpdateInteractionText;

    /// <summary>
    /// The options to show when the interactable object is interacted with.
    /// The first option is always the default one that is chosen if the player does not hold interact to select a different option.
    /// </summary>
    [field: SerializeField] public List<InteractableOption> Options { get; set; } = [];

    private float? heldTime;
    private bool showingOptions;
    private InteractableOption? selectedOption;

    private void Start()
    {
        if (!InteractableObject)
        {
            InteractableObject = GetComponentInChildren<InteractableObject>() ?? throw new InvalidOperationException("No InteractableObject component found in self or children");
            InteractableObject.onInteractStart.AddListener(OnInteractStart);
            InteractableObject.onInteractEnd.AddListener(OnInteractEnd);
        }

        PauseMenu.Instance.onPause.AddListener(OnGamePause);

        UpdateInteractionText();
    }

    private void OnInteractStart()
    {
        Plugin.Logger.LogInfo("Interact start");
        heldTime = 0;
    }

    private void OnInteractEnd()
    {
        Plugin.Logger.LogInfo("Interact end");

        if (heldTime < MaxHoldTimeBeforeShowOptions)
            selectedOption = Options.FirstOrDefault();

        heldTime = null;
        showingOptions = false;
        OnHideOptions();
        LockInput();

        if (selectedOption == null)
            return;

        if (selectedOption.Id == null)
        {
            Plugin.Logger.LogWarning($"Selected option '{selectedOption.Name}' has no id, skipping interaction");
            return;
        }

        OnInteract?.Invoke(selectedOption.Id);
        selectedOption = null;

        UpdateInteractionText();
    }

    private void OnGamePause()
    {
        if (showingOptions)
            OnHideOptions();
    }

    public virtual void OnShowOptions()
    {
        RadialMenu.Instance.Show(Options, OnOptionSelected);
    }

    public virtual void OnHideOptions()
    {
        RadialMenu.Instance.Hide();
    }

    private void LockInput()
    {
        PlayerCamera.Instance.SetCanLook(true);
        PlayerCamera.Instance.LockMouse();
    }

    private void UnlockInput()
    {
        PlayerCamera.Instance.SetCanLook(false);
        PlayerCamera.Instance.FreeMouse();
    }

    private void UpdateInteractionText()
    {
        if (!InteractableObject)
            return;

        var option = Options.FirstOrDefault();
        var eventRefData = new EventRefData<string>(option?.Name);

        OnUpdateInteractionText?.Invoke(option, eventRefData);
        InteractableObject.SetMessage(eventRefData.Value);
    }

    private void Update()
    {
        if (heldTime.HasValue)
        {
            heldTime += Time.deltaTime;

            if (heldTime >= MaxHoldTimeBeforeShowOptions && !showingOptions)
            {
                showingOptions = true;
                OnShowOptions();
                UnlockInput();
            }
        }
    }

    private void OnOptionSelected(InteractableOption option)
    {
        selectedOption = option;
        InteractionManager.Instance.interactedObject.EndInteract();
        InteractionManager.Instance.interactedObject = null;
    }
}

[Serializable]
[CreateAssetMenu(fileName = "Option", menuName = "RealRadio/Interactable Options/Option")]
public class InteractableOption : ScriptableObject
{
    public string? Id;
    public string? Name;
    public Sprite? Sprite;

    /// <summary>
    /// Creates an option. Meant to be used from code at runtime.
    /// </summary>
    public static InteractableOption CreateOption(string id, string name, Sprite? sprite)
    {
        var result = CreateInstance<InteractableOption>();
        result.Id = id;
        result.Name = name;
        result.Sprite = sprite;
        return result;
    }
}
