using System;
using System.Collections.Generic;
using System.Linq;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using UnityEngine;

namespace RealRadio.Components;

public class AudioStreamManager : MonoBehaviour
{
    private Dictionary<string, StreamAudioHost> hosts = new();

    public static AudioStreamManager Instance => GetOrInitInstance();
    private static AudioStreamManager? cachedInstance;

    private static AudioStreamManager GetOrInitInstance()
    {
        cachedInstance ??= FindObjectOfType<AudioStreamManager>() ?? new GameObject("AudioStreamManager").AddComponent<AudioStreamManager>();
        return cachedInstance;
    }

    public StreamAudioHost GetOrCreateHost(string url)
    {
        if (hosts.TryGetValue(url, out var host))
            return host;

        host = CreateHost(url);
        hosts.Add(url, host);
        return host;
    }

    /// <summary>
    /// Removes a host from the manager.
    ///
    /// Note: Don't call this. It's called automatically when an audio host component is removed.
    /// </summary>
    /// <param name="host"></param>
    public void RemoveHost(StreamAudioHost host)
    {
        // optimize this? probably not necessary...
        string? key = hosts.FirstOrDefault(x => x.Value == host).Key;

        if (key != null)
            hosts.Remove(key);
    }

    private StreamAudioHost CreateHost(string url)
    {
        var go = new GameObject("AudioStreamHost");
        go.transform.SetParent(transform);

        var host = go.AddComponent<StreamAudioHost>();
        host.AudioStream = new MediaFoundationAudioStream(url, resetReaderAtEof: false)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };

        return host;
    }

    private void Awake()
    {
        if (cachedInstance != null)
        {
            UnityEngine.Debug.LogWarning("An instance of AudioStreamManager already exists");
            Destroy(this);
            return;
        }
    }

    private void OnDestroy()
    {
        if (cachedInstance == this)
        {
            cachedInstance = null;
        }

        foreach (var host in hosts.Values)
            Destroy(host.gameObject);
    }
}
