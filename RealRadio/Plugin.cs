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

    private Harmony? harmony;

    private bool visitedMenu;

    private void Awake()
    {
        Logger = base.Logger;

        harmony = new Harmony("com.skipcast.realradio");
        harmony.PatchAll();

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        SceneManager.activeSceneChanged += (oldScene, newScene) =>
        {
            if (newScene.name == "Menu" && !visitedMenu)
            {
                visitedMenu = true;
                LoadManager.Instance.onLoadComplete.AddListener(this, AccessTools.Method(GetType(), nameof(OnMainSceneLoadComplete)));
            }

            if (newScene.name == "Main")
            {
                var go = new GameObject("RadioSpawner");
                go.AddComponent<Components.Debugging.RadioSpawner>();

                //var shopNpc = FindObjectOfType<Dan>();
                //shopNpc.ShopInterface.CreateListingUI(AssetRegistry.ShopListings.RadioTier1);
            }
        };
    }

    private void OnMainSceneLoadComplete()
    {
        if (InstanceFinder.IsServer)
        {
            CreateSingletons();
        }
    }

    private void CreateSingletons()
    {
        Logger.LogInfo("Creating singletons");
        InstanceFinder.ServerManager.Spawn(Instantiate(AssetRegistry.SingletonPrefabs.OffGridBuildManager));
    }
}
