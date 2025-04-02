using AudioStreamer;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using UnityEngine;

namespace RealRadio.Components.Debug;

public class RadioSpawner : MonoBehaviour
{
    private AudioStream? audioStream;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Plugin.Logger.LogInfo("Try spawn radio");
            TrySpawnRadio();
        }
    }

    private StreamAudioSource? TrySpawnRadio()
    {
        var cameraTransform = Camera.main.transform;
        var lookDirection = cameraTransform.forward;
        var ray = new Ray(cameraTransform.position, lookDirection);
        var layerMask = Layers.All;
        layerMask &= ~Layers.Player;

        if (Physics.Raycast(ray, out var hit, 3f, layerMask.ToLayerMask()))
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = Vector3.one * 0.2f;
            go.transform.position = hit.point;
            go.transform.rotation.SetLookRotation(lookDirection);

            if (audioStream == null)
            {
                audioStream = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: true)
                {
                    ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: 44100, channels: 2),
                };
            }

            var audioGo = StreamAudioSource.CreateGameObject(audioStream, false, go.transform).gameObject;
            var audioSource = audioGo.GetComponent<AudioSource>();
            audioSource.volume = 0.05f;
            audioSource.spatialBlend = 1f;

            Plugin.Logger.LogInfo($"Spawned radio at {go.transform.position} (hit {hit.transform.gameObject.name}, layer {hit.collider.gameObject.layer})");
        }

        return null;
    }
}
