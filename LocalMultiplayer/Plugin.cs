using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LocalMultiplayer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger = null!;

    private static Harmony? harmony;

    private void Awake()
    {
        Logger = base.Logger;

        harmony = new Harmony("com.skipcast.localmultiplayer");
        harmony.PatchAll();

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
}
