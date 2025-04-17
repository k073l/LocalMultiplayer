using System;
using System.Collections;
using AudioStreamer;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using RealRadio.Components.Audio;
using ScheduleOne.NPCs;
using UnityEngine;

namespace RealRadio.Components.Debugging;

public class RadioSpawner : MonoBehaviour
{
    private StreamAudioHost? audioHost;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Plugin.Logger.LogInfo("Try spawn radio");
            TrySpawnRadio();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            SpawnRadioOnAllNpcs();
        }
    }

    private void TrySpawnRadio()
    {
        var cameraTransform = Camera.main.transform;
        var lookDirection = cameraTransform.forward;
        var ray = new Ray(cameraTransform.position, lookDirection);
        var layerMask = Layers.All;
        layerMask &= ~Layers.Player;

        if (Physics.Raycast(ray, out var hit, 10f, layerMask.ToLayerMask()))
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(hit.transform, worldPositionStays: true);
            go.transform.localScale = Vector3.one * 0.2f;
            go.transform.position = hit.point;
            go.transform.rotation.SetLookRotation(lookDirection);

            var audioHost = GetOrCreateAudioHost();
            var audioClient = audioHost.CreateClient(parent: go.transform);
            audioClient.ConvertToMono = true;

            Plugin.Logger.LogInfo($"Spawned radio at {go.transform.position} (hit {hit.transform.gameObject.name}, layer {hit.collider.gameObject.layer})");
        }
    }

    private StreamAudioHost GetOrCreateAudioHost()
    {
        if (audioHost == null)
        {
            audioHost = AudioStreamManager.Instance.GetOrCreateHost("https://stream.simulatorradio.com");
        }

        return audioHost;
    }

    /*private void CreateAudioStreamIfNeeded()
    {
        if (audioHost == null)
        {
            audioHost = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: true)
            {
                ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: 44100, channels: 2),
            };
        }
    }*/

    private void SpawnRadioOnAllNpcs()
    {
        var npcs = FindObjectsOfType<NPC>();

        foreach (var npc in npcs)
        {
            if (npc.gameObject.GetComponentInChildren<StreamAudioHost>())
                continue;

            //CreateAudioStreamIfNeeded();

            /*var audioStream = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: true)
            {
                ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
            };

            var audioGo = StreamAudioHost.CreateGameObject(audioStream!, false, npc.transform).gameObject;
            var audioSource = audioGo.GetComponent<AudioSource>();
            audioSource.volume = 0.2f;
            audioSource.spatialBlend = 1f;*/
        }
    }
}
