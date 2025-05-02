using System;
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

namespace RealRadio.Components.Radio;

public class VehicleRadioManager : NetworkSingleton<VehicleRadioManager>
{
    private bool radialMenuOpen;

    private InteractableOption[]? radialMenuOptions;

    public override void Awake()
    {
        base.Awake();

        // Call OnVehicleSpawned for pre-existing vehicles
        foreach (var vehicle in FindObjectsOfType<LandVehicle>())
        {
            OnVehicleSpawned(vehicle);
        }

        LandVehicleStartPatch.OnVehicleSpawned += OnVehicleSpawned;
    }

    public override void Start()
    {
        radialMenuOptions = CreateRadialMenuOptions();
    }

    private InteractableOption[] CreateRadialMenuOptions()
    {
        var options = new InteractableOption[RadioStationManager.Instance.Stations.Count];

        for (int i = 0; i < RadioStationManager.Instance.Stations.Count; ++i)
        {
            var station = RadioStationManager.Instance.Stations[i];
            options[i] = InteractableOption.CreateOption(
                id: i.ToString(CultureInfo.InvariantCulture),
                name: station.Name!,
                sprite: null
            );
        }

        return options;
    }

    private void OnVehicleSpawned(LandVehicle vehicle)
    {
        if (!IsServer)
            return;

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
        if (GameInput.GetButtonUp(GameInput.ButtonCode.Reload) || Player.Local.CurrentVehicle == null)
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

        if (radialMenuOpen)
            return;

        if (radialMenuOptions == null)
        {
            Plugin.Logger.LogWarning("Radio station options are null");
            return;
        }

        radialMenuOpen = true;
        RadialMenu.Instance.Show(radialMenuOptions, onOptionSelected: OnRadialOptionSelected);
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
