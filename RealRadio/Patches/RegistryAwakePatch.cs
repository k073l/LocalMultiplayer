namespace RealRadio.Patches;

using HarmonyLib;
using RealRadio.Assets;
using ScheduleOne;

[HarmonyPatch(typeof(Registry), nameof(Registry.Awake))]
public static class RegistryAwakePatch
{
    public static void Prefix(Registry __instance)
    {
        AssetRegistry.Initialize(__instance);
    }
}
