using System;
using RealRadio.Components.Vehicles;
using RealRadio.Patches;
using ScheduleOne.DevUtilities;
using ScheduleOne.Vehicles;

namespace RealRadio.Components.Radio;

public class VehicleRadioManager : NetworkSingleton<VehicleRadioManager>
{
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

    private void OnVehicleSpawned(LandVehicle vehicle)
    {
        var proxyPrefab = Plugin.Assets!.Prefabs.VehicleRadioProxy;
        var proxy = Instantiate(proxyPrefab, parent: transform);
        proxy.GetComponent<VehicleRadioProxy>().Vehicle = vehicle;
        Spawn(proxy);
    }
}
