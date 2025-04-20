using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using FishNet;
using HarmonyLib;
using RealRadio.Assets;
using ScheduleOne.NPCs.CharacterClasses;
using ScheduleOne.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRadio;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger { get; private set; } = null!;

    public static AssetRegistry? Assets
    {
        get => assets;
        internal set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (assets != null)
                throw new InvalidOperationException("Assets have already been set");

            assets = value;
        }
    }
    public static AssetBundle AssetBundle { get; private set; } = null!;

    private Harmony? harmony;

    private bool visitedMenu;
    private static AssetRegistry? assets;

    private void Awake()
    {
        Logger = base.Logger;

        AssetBundle = LoadAssetBundle();
        Logger.LogInfo($"Loaded asset bundle: {AssetBundle.name}");

        foreach (var path in AssetBundle.GetAllAssetNames())
        {
            Logger.LogInfo($"- Found asset: {path}");
        }

        harmony = new Harmony("com.skipcast.realradio");
        harmony.PatchAll();

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        SceneManager.activeSceneChanged += (oldScene, newScene) =>
        {
            if (newScene.name == "Menu" && !visitedMenu)
            {
                visitedMenu = true;
                LoadManager.Instance.onLoadComplete.AddListener(OnMainSceneLoadComplete);
                CreatePersistentSingletons();
            }

            if (newScene.name == "Main")
            {
                var go = new GameObject("RadioSpawner");
                go.AddComponent<Components.Debugging.RadioSpawner>();
            }
        };
    }

    private AssetBundle LoadAssetBundle()
    {
        string path = GetAssetBundlePath();

        try
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);

            if (assetBundle == null)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Asset bundle file not found", path);
                }
                else
                {
                    throw new Exception("Failed to load asset bundle");
                }
            }

            return assetBundle;
        }
        catch (Exception)
        {
            Logger.LogError("Failed to load asset bundle!");
            throw;
        }
    }

    internal static string GetAssetBundlePath() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "assets");

    private void OnMainSceneLoadComplete()
    {
        if (InstanceFinder.IsServer)
        {
            CreateMainSceneServerSingletons();
        }

        CreateMainSceneClientSingletons();
    }

    private void CreateMainSceneServerSingletons()
    {
        if (Assets == null)
            throw new InvalidOperationException("Assets have not been set");

        Logger.LogInfo("Creating main scene server singletons");
        InstanceFinder.ServerManager.Spawn(Instantiate(Assets.Singletons.OffGridBuildManager));
        InstanceFinder.ServerManager.Spawn(Instantiate(Assets.Singletons.RadioSyncManager));
    }

    private void CreateMainSceneClientSingletons()
    {
        if (Assets == null)
            throw new InvalidOperationException("Assets have not been set");

        Logger.LogInfo("Creating main scene client singletons");
        Instantiate(Assets.Singletons.RadialMenu);
    }

    private void CreatePersistentSingletons()
    {
        if (Assets == null)
            throw new InvalidOperationException("Assets have not been set");

        Logger.LogInfo("Creating persistent singletons");
        Instantiate(Assets.Singletons.RadioStationManager);
    }
}
