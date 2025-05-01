using System;
using HarmonyLib;
using ScheduleOne.Vehicles;

namespace RealRadio.Patches
{
    [HarmonyPatch(typeof(LandVehicle), nameof(LandVehicle.Start))]
    public class LandVehicleStartPatch
    {
        public static Action<LandVehicle>? OnVehicleSpawned;

        public static void Postfix(LandVehicle __instance)
        {
            OnVehicleSpawned?.Invoke(__instance);
        }
    }
}
