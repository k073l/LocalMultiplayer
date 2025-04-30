using System;
using System.Collections.Generic;
using System.Linq;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using UnityEngine;

namespace RealRadio.Components.Audio;

public class AudioStreamManager : MonoBehaviour
{
    public uint MaxActiveInaudibleHosts = 50;

    public int NumActiveHosts => hosts.Count(host => host.Value.AudioStream?.Started == true);

    private Dictionary<string, StreamAudioHost> hosts = new();
    private LinkedList<StreamAudioHost> activeHosts = new();
    private StreamAudioHost[]? activeHostsBuffer;

    public static AudioStreamManager Instance => GetOrInitInstance();
    private static AudioStreamManager? cachedInstance;

    private static AudioStreamManager GetOrInitInstance()
    {
        if (!cachedInstance)
            cachedInstance = null;

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

        host.OnStreamStarted += () => OnHostStreamToggled(host, true);
        host.OnStreamStopped += () => OnHostStreamToggled(host, false);

        return host;
    }

    private void OnHostStreamToggled(StreamAudioHost host, bool started)
    {
        if (started && !activeHosts.Contains(host))
        {
            activeHosts.AddLast(host);
        }
        else if (!started && activeHosts.Contains(host))
        {
            activeHosts.Remove(host);
        }
    }

    private void Awake()
    {
        if (cachedInstance)
        {
            UnityEngine.Debug.LogWarning("An instance of AudioStreamManager already exists");
            Destroy(this);
            return;
        }
    }

    private void Update()
    {
        int numActiveHosts = NumActiveHosts;

        while (numActiveHosts > MaxActiveInaudibleHosts)
        {
            if (activeHostsBuffer == null || activeHostsBuffer.Length < numActiveHosts)
            {
                activeHostsBuffer = new StreamAudioHost[numActiveHosts];
            }

            activeHosts.CopyTo(activeHostsBuffer, 0);

            foreach (var host in activeHostsBuffer)
            {
                if (host == null)
                    continue;

                if (host.NumActiveClients == 0)
                {
                    Plugin.Logger.LogInfo("Killing quiet audio host before inactive timer expires due to too many active hosts");

                    host.StopAudioStream();
                    --numActiveHosts;

                    if (numActiveHosts <= MaxActiveInaudibleHosts)
                        break;
                }
            }
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
