using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using CommandLine;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LocalMultiplayer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger = null!;
    internal static LaunchArguments? LaunchArguments { get; private set; }

    private static Harmony? harmony;

    private void Awake()
    {
        Logger = base.Logger;

        harmony = new Harmony("com.skipcast.localmultiplayer");
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

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        SceneManager.activeSceneChanged += (oldScene, newScene) =>
        {
            if (newScene.name == "Menu")
            {
                if (FindObjectOfType<MenuComponent>() != null)
                    return;

                var go = new GameObject("LocalMultiplayerSpawner");
                go.AddComponent<MenuComponent>();
            }
        };
    }

    private void ParseLaunchArguments()
    {
        var args = Environment.GetCommandLineArgs();
        var parser = new Parser(config =>
        {
            config.IgnoreUnknownArguments = true;
        }).ParseArguments<LaunchArguments>(args).WithParsed(args =>
        {
            LaunchArguments = args;
        })
        .WithNotParsed(errors =>
        {
            Logger.LogError("Failed to parse launch arguments");
            foreach (var error in errors)
            {
                Logger.LogError(error.ToString());
            }
        });
    }

    public static void SetWindowPositionSize()
    {
        if (LaunchArguments == null)
            return;

        int offsetLeft = 1000;
        var mainDisplay = Screen.GetMainWindowDisplayInfo();
        RectInt workArea = mainDisplay.workArea;
        workArea.width -= offsetLeft;
        var windowSize = new Vector2Int(workArea.width / 2, workArea.height);
        Vector2Int position = Vector2Int.zero;

        if (LaunchArguments.Host)
        {
            position.x = offsetLeft;
        }
        else if (LaunchArguments.Join)
        {
            position.x = (workArea.width / 2) + offsetLeft;
        }

        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.fullScreen = false;
        Screen.SetResolution(windowSize.x, windowSize.y, fullscreen: false);
        Screen.MoveMainWindowTo(mainDisplay, position);
    }
}

internal class LaunchArguments
{
    [Option('h', "host", SetName = "mode")]
    public bool Host { get; set; }

    [Option('j', "join", SetName = "mode")]
    public bool Join { get; set; }

    [Option('s', "adjust-window", Default = true)]
    public bool SetWindowPositionSize { get; set; }
}
