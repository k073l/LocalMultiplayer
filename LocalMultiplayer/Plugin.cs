using System.Reflection;
using HarmonyLib;
using UnityEngine;
using MelonLoader;
using LocalMultiplayer;
using Object = UnityEngine.Object;

[assembly:
    MelonInfo(typeof(LocalMultiplayer.LocalMultiplayer), BuildInfo.Name, BuildInfo.Version,
        BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: HarmonyDontPatchAll]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LocalMultiplayer;

public static class BuildInfo
{
    public const string Name = "LocalMultiplayer";
    public const string Author = "Skippeh (ported by k073l)";
    public const string Version = "1.0";
    public const string DownloadLink = null;
}

public class LocalMultiplayer : MelonMod
{
    internal static MelonLogger.Instance Logger = null!;
    internal static LaunchArguments? LaunchArguments { get; private set; }

    private static HarmonyLib.Harmony? harmony;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        harmony = new HarmonyLib.Harmony("com.skipcast.localmultiplayer");
        harmony.PatchAll();

        ParseLaunchArguments();

        if (LaunchArguments?.SetWindowPositionSize == true)
        {
            SetWindowPositionSize();
        }

        if (LaunchArguments?.Host == true)
        {
            Console.Title += " (SERVER)";
        }
        else if (LaunchArguments?.Join == true)
        {
            Console.Title += " (CLIENT)";
        }

        Logger.Msg($"Mod {BuildInfo.Name} is loaded!");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Menu")
        {
            if (Object.FindObjectOfType<MenuComponent>() != null)
                return;
            var go = new GameObject("LocalMultiplayerSpawner");
            go.AddComponent<MenuComponent>();

            if (Object.FindObjectOfType<AudioComponent>() == null)
            {
                var go2 = new GameObject("LocalMultiplayerAudioController");
                go2.AddComponent<AudioComponent>();
                Object.DontDestroyOnLoad(go2);
            }
        }
    }

    private void ParseLaunchArguments()
    {
        var args = Environment.GetCommandLineArgs();
        var launchArgs = new LaunchArguments();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host":
                case "-h":
                    launchArgs.Host = true;
                    break;
                case "--join":
                case "-j":
                    launchArgs.Join = true;
                    break;
                case "--adjust-window":
                case "-s":
                    launchArgs.SetWindowPositionSize = true;
                    break;
                case "--left-offset":
                case "-o":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int offset))
                    {
                        launchArgs.LeftOffset = offset;
                        i++;
                    }

                    break;
            }
        }

        LaunchArguments = launchArgs;
    }

    public static void SetWindowPositionSize()
    {
        if (LaunchArguments == null)
            return;

        int offsetLeft = Math.Max(0, LaunchArguments.LeftOffset);

        var getDisplayInfoMethod = AccessTools.Method(typeof(Screen), "GetMainWindowDisplayInfo", Type.EmptyTypes);
        if (getDisplayInfoMethod == null)
        {
            Logger.Warning("Could not find Screen.GetMainWindowDisplayInfo()");
            return;
        }

        var mainDisplay = getDisplayInfoMethod.Invoke(null, null);
        if (mainDisplay == null)
        {
            Logger.Warning("Failed to get main display info");
            return;
        }

        var workAreaField = mainDisplay.GetType().GetField("workArea", BindingFlags.Public | BindingFlags.Instance);
        if (workAreaField == null)
        {
            Logger.Warning("Could not access workArea field");
            return;
        }

        RectInt workArea = (RectInt)workAreaField.GetValue(mainDisplay)!;

        if (offsetLeft > workArea.width)
        {
            Logger.Warning("Left offset is greater than the width of the screen, cancelling window adjustment");
            return;
        }

        workArea.width -= offsetLeft;
        var windowSize = new Vector2Int(workArea.width / 2, workArea.height);
        Vector2Int position = Vector2Int.zero;

        if (LaunchArguments.Host)
            position.x = offsetLeft;
        else if (LaunchArguments.Join)
            position.x = (workArea.width / 2) + offsetLeft;

        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.fullScreen = false;
        Screen.SetResolution(windowSize.x, windowSize.y, fullscreen: false);
        var mainDisplayInfo = (DisplayInfo)mainDisplay;
#if MONO
        Screen.MoveMainWindowTo(mainDisplayInfo, position);
#else
        Screen.MoveMainWindowTo(ref mainDisplayInfo, position);
#endif
    }
}

internal class LaunchArguments
{
    public bool Host { get; set; }

    public bool Join { get; set; }

    public bool SetWindowPositionSize { get; set; }

    public int LeftOffset { get; set; }
}