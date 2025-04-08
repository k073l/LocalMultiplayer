using HarmonyLib;
using ScheduleOne.DevUtilities;

namespace LocalMultiplayer.Patches;

[HarmonyPatch(typeof(Settings), nameof(Settings.ApplyDisplaySettings))]
public static class SettingsApplyDisplaySettingsPatch
{
    public static bool Prefix(Settings __instance)
    {
        if (Plugin.LaunchArguments == null || !Plugin.LaunchArguments.SetWindowPositionSize)
            return true;

        Plugin.SetWindowPositionSize();
        return false;
    }
}
