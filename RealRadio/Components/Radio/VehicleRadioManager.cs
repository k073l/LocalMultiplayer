using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RealRadio.Components.Building;
using RealRadio.Components.UI;
using RealRadio.Components.Vehicles;
using RealRadio.Patches;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class VehicleRadioManager : NetworkSingleton<VehicleRadioManager>
{
    public Sprite TurnOffIcon = null!;

    private bool radialMenuOpen;

    private InteractableOption[]? radialMenuOptions;

    public override void Awake()
    {
        base.Awake();

        if (TurnOffIcon == null)
            throw new InvalidOperationException("TurnOffIcon is null");

        // Call OnVehicleSpawned for pre-existing vehicles
        foreach (var vehicle in FindObjectsOfType<LandVehicle>())
        {
            StartCoroutine(OnVehicleSpawned(vehicle));
        }

        LandVehicleStartPatch.OnVehicleSpawned += (vehicle) => StartCoroutine(OnVehicleSpawned(vehicle));
    }

    public override void Start()
    {
        radialMenuOptions = CreateRadialMenuOptions();
    }

    private InteractableOption[] CreateRadialMenuOptions()
    {
        var options = new InteractableOption[RadioStationManager.Instance.Stations.Count + 1];

        options[0] = InteractableOption.CreateOption(
            id: "-1",
            name: "Turn off",
            sprite: TurnOffIcon
        );

        for (int i = 0; i < RadioStationManager.Instance.Stations.Count; ++i)
        {
            var station = RadioStationManager.Instance.Stations[i];
            options[i + 1] = InteractableOption.CreateOption(
                id: i.ToString(CultureInfo.InvariantCulture),
                name: station.Name!,
                sprite: station.Icon,
                abbreviation: station.Abbreviation,
                backgroundColor: station.BackgroundColor,
                textColor: station.TextColor,
                roundedBackground: station.RoundedBackground
            );
        }

        return options;
    }

    private IEnumerator OnVehicleSpawned(LandVehicle vehicle)
    {
        if (!IsServer && !IsClient)
            yield return new WaitUntil(() => IsServer || IsClient);

        if (!IsServer)
            yield break;

        var proxyPrefab = Plugin.Assets!.Prefabs.VehicleRadioProxy;
        var proxy = Instantiate(proxyPrefab, parent: transform);
        proxy.GetComponent<VehicleRadioProxy>().Vehicle = vehicle;
        Spawn(proxy);
    }

    private void Update()
    {
        UpdateRadialMenu();
    }

    private void UpdateRadialMenu()
    {
        if (GameInput.GetButtonUp(GameInput.ButtonCode.Reload) || Player.Local?.CurrentVehicle == null)
        {
            if (radialMenuOpen)
            {
                radialMenuOpen = false;
                RadialMenu.Instance.Hide();
            }

            return;
        }

        if (!GameInput.GetButtonDown(GameInput.ButtonCode.Reload))
            return;

        // Only show the radial menu if the current vehicle has a proxy
        var proxyRef = Player.Local.CurrentVehicle.GetComponent<VehicleRadioProxyReference>();

        if (proxyRef == null)
            return;

        if (radialMenuOpen)
            return;

        if (radialMenuOptions == null)
        {
            Plugin.Logger.LogWarning("Radio station options are null");
            return;
        }

        // +1 because the first option is "turn off"
        var selectedOption = radialMenuOptions[proxyRef.Proxy!.RadioStationIndex + 1];

        radialMenuOpen = true;
        RadialMenu.Instance.Show(radialMenuOptions, onOptionSelected: OnRadialOptionSelected, selectedOption: selectedOption);
    }

    private void OnRadialOptionSelected(InteractableOption option)
    {
        if (!int.TryParse(option.Id, out var index))
        {
            Plugin.Logger.LogWarning($"Failed to parse option index: {option.Id}");
            return;
        }

        var proxyRef = Player.Local.CurrentVehicle.GetComponent<VehicleRadioProxyReference>();
        proxyRef?.Proxy?.SetRadioStationIndex(index);

        radialMenuOpen = false;
        RadialMenu.Instance.Hide();
    }
}
