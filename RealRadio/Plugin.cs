using AudioStreamer;
using AudioStreamer.MediaFoundation;
using BepInEx;
using BepInEx.Logging;
using NAudio.Wave;
using RealRadio.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRadio;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger = null!;

    private AudioStream? audioStream;

    private void Awake()
    {
        Logger = base.Logger;

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        SceneManager.activeSceneChanged += (oldScene, newScene) =>
        {
            if (newScene.name == "Menu")
            {
                Logger.LogInfo("Creating test audio object");

                audioStream = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: true)
                {
                    ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
                };
                audioStream.Start();

                var audioSource = new GameObject("RadioAudioSource").AddComponent<RadioAudioSource>();
                audioSource.AudioReader = audioStream.CreateReader();
                audioSource.ConvertToMono = true;
            }
        };
    }
}
