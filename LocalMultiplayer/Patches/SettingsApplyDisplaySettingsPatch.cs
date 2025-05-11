using HarmonyLib;

#if MONO
using ScheduleOne.DevUtilities;
#else
using Il2CppScheduleOne.DevUtilities;
#endif

namespace LocalMultiplayer.Patches;

[HarmonyPatch(typeof(Settings), nameof(Settings.ApplyDisplaySettings))]
public static class SettingsApplyDisplaySettingsPatch
{
    public static bool Prefix(Settings __instance)
    {
        if (LocalMultiplayer.LaunchArguments == null || !LocalMultiplayer.LaunchArguments.SetWindowPositionSize)
            return true;

        LocalMultiplayer.SetWindowPositionSize();
        return false;
    }
}
