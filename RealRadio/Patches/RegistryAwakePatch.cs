namespace RealRadio.Patches;

using FishNet;
using HarmonyLib;
using RealRadio.Assets;
using ScheduleOne;

[HarmonyPatch(typeof(Registry), nameof(Registry.Awake))]
public static class RegistryAwakePatch
{
    public static void Prefix(Registry __instance)
    {
        if (Plugin.Assets == null)
            Plugin.Assets = AssetRegistry.LoadFromAssetBundle(Plugin.AssetBundle);

        AssetRegistry.Register(__instance, InstanceFinder.NetworkManager, Plugin.AssetBundle);
    }
}
