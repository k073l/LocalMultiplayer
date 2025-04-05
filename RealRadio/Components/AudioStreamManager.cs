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

    public static AudioStreamManager Instance => FindObjectOfType<AudioStreamManager>() ?? new GameObject("AudioStreamManager").AddComponent<AudioStreamManager>();

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
    /// Note: Don't call this. It's called automatically when a host component is removed or parent gameobject is destroyed.
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
        host.AudioStream = new MediaFoundationAudioStream(url, resetReaderAtEof: true)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };

        return host;
    }

    private void OnDestroy()
    {
        foreach (var host in hosts.Values)
            Destroy(host.gameObject);
    }
}
