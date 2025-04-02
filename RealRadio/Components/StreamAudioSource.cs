using System;
using System.Threading;
using System.Threading.Tasks;
using AudioStreamer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace RealRadio.Components;

[RequireComponent(typeof(AudioSource))]
public class StreamAudioSource : MonoBehaviour
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

    private AudioClip? audioClip;
    private AudioSource? audioSource;
    private ISampleProvider? sampler;
    private AudioStreamReader? audioReader;
    private WaveBuffer? waveBuffer;
    private bool convertToMonoDirty;
    private bool convertToMono;
    private Task? startStreamTask;
    private CancellationTokenSource? startStreamCts;

    public static StreamAudioSource CreateGameObject(AudioStream audioStream, bool convertToMono = false, Transform? parent = null, Vector3? position = null)
    {
        if (audioStream == null)
            throw new ArgumentNullException(nameof(audioStream));

        if (audioStream.IsDisposed)
            throw new ArgumentException("AudioStream is disposed", nameof(audioStream));

        var go = new GameObject("StreamAudioClip");

        if (parent != null)
            go.transform.SetParent(parent);

        if (position != null)
            go.transform.localPosition = position.Value;

        var audioClip = go.AddComponent<StreamAudioSource>();
        audioClip.AudioStream = audioStream;
        audioClip.ConvertToMono = convertToMono;
        return audioClip;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? throw new InvalidOperationException("No AudioSource component found on game object");
        audioSource.loop = true;
        audioSource.volume = 0.05f;
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

        if (sampler is IDisposable disposable)
            disposable.Dispose();

        audioReader?.Dispose();
        AudioStream?.Dispose();
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
        }, startStreamCts.Token);
    }

    private void CheckStartStreamTask()
    {
        if (startStreamTask == null)
            return;

        if (startStreamCts?.IsCancellationRequested == true)
            return;

        if (!startStreamTask.IsCompleted)
            return;

        if (startStreamTask.Exception != null)
            throw startStreamTask.Exception;

        startStreamTask = null;
        startStreamCts?.Dispose();
        startStreamCts = null;

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

        AudioClip = AudioClip.Create("StreamedAudio", 4096, sampler.WaveFormat.Channels, sampler.WaveFormat.SampleRate, stream: true, PCMReaderCallback);
    }

    private void PCMReaderCallback(float[] floatData)
    {
        if (AudioStream == null || !AudioStream.Started || sampler == null)
            return;

        sampler.Read(floatData, 0, floatData.Length);
    }
}
