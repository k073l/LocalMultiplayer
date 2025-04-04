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
    public Action<AudioClip>? OnAudioClipChanged;

    public AudioStream? AudioStream;

    public AudioClip? AudioClip
    {
        get => audioClip;
        private set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "A value must be provided");

            if (audioClip == value)
                return;

            audioClip = value;
            OnAudioClipChanged?.Invoke(audioClip);
        }
    }

    public bool ConvertToMono
    {
        get => convertToMono;
        set
        {
            if (value == convertToMono)
                return;

            convertToMono = value;
            convertToMonoDirty = true;
        }
    }

    public float[]? AudioData { get; private set; }
    public int AudioDataLength { get; private set; }
    private int audioDataPosition;
    private DateTime lastCallbackTime = DateTime.MinValue;

    private AudioClip? audioClip;
    private AudioSource? audioSource;
    private ISampleProvider? sampler;
    private AudioStreamReader? audioReader;
    private bool convertToMonoDirty;
    private bool convertToMono;
    private Task? startStreamTask;
    private CancellationTokenSource? startStreamCts;
    private List<StreamAudioClient> spawnedClients = [];
    private int clientIdCounter;

    public StreamAudioClient CreateClient(Transform? parent = null, Vector3? localPosition = null)
    {
        var go = new GameObject("StreamAudioClient");

        if (parent != null)
            go.transform.SetParent(parent ?? transform, false);

        if (localPosition != null)
            go.transform.localPosition = localPosition ?? Vector3.zero;

        var client = go.AddComponent<StreamAudioClient>();
        client.Host = this;
        client.Id = clientIdCounter++;
        var audioSource = client.GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.priority = 0;
        audioSource.volume = 0;

        if (transform != null || localPosition != null)
        {
            StartCoroutine(FixAudioStutter(audioSource));
        }

        spawnedClients.Add(client);

        return client;
    }

    private IEnumerator<YieldInstruction?> FixAudioStutter(AudioSource audioSource)
    {
        yield return null;
        audioSource.spatialBlend = 1f;
        yield return null;
        audioSource.spatialBlend = 0f;
        yield return null;
        audioSource.spatialBlend = 1f;
        yield return null;
        audioSource.volume = 0.2f;
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
        audioSource.loop = true;
        audioSource.volume = 0;
        audioSource.playOnAwake = false;

        OnAudioClipChanged += clip =>
        {
            audioSource.clip = clip;
            audioSource.Play();
        };
    }

    private void Start()
    {
        if (audioReader == null && startStreamTask == null)
            OnEnable();
    }

    private void Update()
    {
        if (convertToMonoDirty)
        {
            convertToMonoDirty = false;
            UpdateAudioClip();
        }

        CheckStartStreamTask();

        if (audioSource != null)
            audioSource.volume = 0;
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
        else
        {
            OnAudioStreamStarted();
        }
    }

    private void OnDisable()
    {
        audioSource?.Stop();

        if (AudioStream != null && audioReader != null)
        {
            AudioStream.RemoveReader(audioReader);
            audioReader.Dispose();
            audioReader = null;
        }

        if (sampler is IDisposable disposable)
            disposable.Dispose();

        sampler = null;

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

        if (AudioStream != null && AudioStream.NumReaders == 0)
            AudioStream.Stop();
    }

    private void OnDestroy()
    {
        Destroy(audioClip);

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

        startStreamTask = null;
        startStreamCts?.Dispose();
        startStreamCts = null;

        if (AudioStream != null && AudioStream.Started)
            OnAudioStreamStarted();
    }

    private void OnAudioStreamStarted()
    {
        if (AudioStream == null || !AudioStream.Started)
            throw new InvalidOperationException("AudioStream is not started");

        if (audioReader != null)
            return;

        audioReader = AudioStream.CreateReader();
        UpdateAudioClip();
    }

    private void UpdateAudioClip()
    {
        if (audioReader == null)
            throw new InvalidOperationException("Audio reader is not set");

        if (audioClip)
        {
            Destroy(audioClip);
        }

        if (sampler is IDisposable disposable && sampler != audioReader)
            disposable.Dispose();

        if (ConvertToMono)
        {
            sampler = new StereoToMonoSampleProvider(audioReader);
        }
        else
        {
            sampler = audioReader;
        }

        AudioClip = AudioClip.Create("StreamedAudioHost", 8192, sampler.WaveFormat.Channels, sampler.WaveFormat.SampleRate, stream: true, PCMReaderCallback);
    }

    private void PCMReaderCallback(float[] floatData)
    {
        if ((DateTime.UtcNow - lastCallbackTime) > TimeSpan.FromMilliseconds(50))
        {
            audioDataPosition = 0;
            AudioDataLength = 0;
        }

        lastCallbackTime = DateTime.UtcNow;

        if (AudioStream == null || !AudioStream.Started || sampler == null)
        {
            Array.Fill(floatData, 0);
            return;
        }

        var numFloatsRead = sampler.Read(floatData, 0, floatData.Length);
        int newDataSize = audioDataPosition + numFloatsRead;
        if (AudioData == null || AudioData.Length < newDataSize)
        {
            Plugin.Logger.LogInfo($"host: Increasing audio data buffer size to {newDataSize} bytes");
            var newData = new float[newDataSize];

            if (AudioData != null)
            {
                // Copy old data
                Array.Copy(AudioData, newData, length: audioDataPosition);
            }

            AudioData = newData;
        }

        //Plugin.Logger.LogInfo($"host: Copying {numFloatsRead} from index {audioDataPosition} into buffer of size {AudioData.Length}");

        Array.Copy(
            sourceArray: floatData,
            sourceIndex: 0,
            destinationArray: AudioData,
            destinationIndex: audioDataPosition,
            length: numFloatsRead
        );

        AudioDataLength += numFloatsRead;
        audioDataPosition += numFloatsRead;
    }
}
