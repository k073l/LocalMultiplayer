using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioStreamer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ScheduleOne.NPCs.CharacterClasses;
using UnityEngine;

namespace RealRadio.Components;

[RequireComponent(typeof(AudioSource))]
public class StreamAudioHost : MonoBehaviour
{
    public AudioStream? AudioStream;

    public float[]? AudioData { get; private set; }
    public int AudioDataLength { get; private set; }

    private AudioSource audioSource = null!;
    private Task? startStreamTask;
    private CancellationTokenSource? startStreamCts;
    private List<StreamAudioClient> spawnedClients = [];
    private HashSet<StreamAudioClient> enabledClients = [];
    private int clientIdCounter;

    public StreamAudioClient CreateClient(Transform? parent = null, Vector3? localPosition = null)
    {
        var go = new GameObject("StreamAudioClient");

        if (parent != null)
            go.transform.SetParent(parent ?? transform, false);

        if (localPosition != null)
            go.transform.localPosition = localPosition ?? Vector3.zero;

        var client = AddClient(go);

        if (parent != null || localPosition != null)
        {
            audioSource.spatialBlend = 1;
        }

        return client;
    }

    public StreamAudioClient AddClient(GameObject gameObject)
    {
        bool createdAudioSource = false;
        var audioSource = gameObject.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            createdAudioSource = true;
        }

        var client = gameObject.GetComponent<StreamAudioClient>() ?? gameObject.AddComponent<StreamAudioClient>();
        client.Host = this;
        client.Id = clientIdCounter++;
        audioSource.playOnAwake = false;

        if (createdAudioSource)
        {
            audioSource.volume = 0.2f;
        }

        spawnedClients.Add(client);

        if (client.isActiveAndEnabled)
        {
            enabledClients.Add(client);
        }

        return client;
    }

    public void DestroyClient(StreamAudioClient client)
    {
        if (spawnedClients.Remove(client))
        {
            Destroy(client);
        }
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? throw new InvalidOperationException("No AudioSource component found on game object");
    }

    private void Start()
    {
        if (AudioStream?.Started == false && startStreamTask == null)
            OnEnable();
    }

    private void Update()
    {
        CheckStartStreamTask();
    }

    private void OnEnable()
    {
        if (AudioStream == null)
        {
            Plugin.Logger.LogWarning("AudioStream is null");
            return;
        }

        if (!AudioStream.Started)
            StartAudioStream();
    }

    private void OnDisable()
    {
        audioSource?.Stop();
        AudioStream?.Stop();

        if (startStreamCts != null)
        {
            startStreamCts.Cancel();

            try
            {
                startStreamTask?.Wait();
            }
            catch (AggregateException)
            {
                // Ignore any exceptions thrown in task
            }

            startStreamTask = null;
            startStreamCts.Dispose();
            startStreamCts = null;
        }
    }

    private void OnDestroy()
    {
        foreach (var client in spawnedClients)
            Destroy(client.gameObject);
    }

    private void StartAudioStream()
    {
        if (startStreamTask != null)
            throw new InvalidOperationException("AudioStream is already starting");

        startStreamCts = new CancellationTokenSource();
        startStreamTask = Task.Run(() =>
        {
            if (AudioStream == null)
                throw new InvalidOperationException("AudioStream is not set");

            AudioStream.Start();
            AudioStream.WarmupReader();
        }, startStreamCts.Token);
    }

    private void CheckStartStreamTask()
    {
        if (startStreamTask == null)
            return;

        if (!startStreamTask.IsCompleted)
            return;

        if (startStreamTask.Exception != null)
        {
            Plugin.Logger.LogError("Error starting audio stream");
            Plugin.Logger.LogError(startStreamTask.Exception);
        }

        if (!audioSource.isPlaying)
            audioSource.Play();

        startStreamTask = null;
        startStreamCts?.Dispose();
        startStreamCts = null;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (AudioStream == null || !AudioStream.StreamAvailable)
        {
            Array.Fill(data, 0);
            return;
        }

        if (AudioStream.WaveFormat.Channels != channels)
        {
            // bepinex logger doesn't work here (doesn't work on audio thread i guess?), so use unity logger
            UnityEngine.Debug.LogError($"Channels mismatch: audio stream has {AudioStream.WaveFormat.Channels} channels but unity wants {channels}");
            return;
        }

        if (AudioData == null || AudioData.Length < data.Length)
        {
            AudioData = new float[data.Length];
        }

        var numFloatsRead = AudioStream.Read(AudioData, 0, data.Length);
        AudioDataLength = numFloatsRead;
    }

    internal void OnClientEnabled(StreamAudioClient client)
    {
        enabledClients.Add(client);
        OnNumActiveClientsChanged();
    }

    internal void OnClientDisabled(StreamAudioClient client)
    {
        enabledClients.Remove(client);
        OnNumActiveClientsChanged();
    }

    private void OnNumActiveClientsChanged()
    {
        Plugin.Logger.LogInfo($"Num active clients: {enabledClients.Count}");

        if (enabledClients.Count > 0 && (AudioStream == null || !AudioStream.Started))
        {
            Plugin.Logger.LogInfo("Starting audio stream");
            StartAudioStream();
        }
        else if (enabledClients.Count == 0)
        {
            Plugin.Logger.LogInfo("Stopping audio stream");
            AudioStream?.Stop();
            audioSource.Stop();
        }
    }
}
