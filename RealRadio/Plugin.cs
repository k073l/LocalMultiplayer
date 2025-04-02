using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRadio;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger = null!;

    private void Awake()
    {
        Logger = base.Logger;

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        SceneManager.activeSceneChanged += (oldScene, newScene) =>
        {
            if (newScene.name == "Main")
            {
                var go = new GameObject("RadioSpawner");
                go.AddComponent<Components.Debug.RadioSpawner>();
            }
        };
    }
}
