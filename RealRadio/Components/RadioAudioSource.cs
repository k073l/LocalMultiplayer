using System;
using AudioStreamer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ScheduleOne.DevUtilities;
using ScheduleOne.NPCs.CharacterClasses;
using UnityEngine;

namespace RealRadio.Components;

public class RadioAudioSource : MonoBehaviour, IDisposable
{
    public bool ConvertToMono
    {
        get => convertToMono;
        set
        {
            if (value == convertToMono)
                return;

            convertToMono = value;

            if (audioReader == null)
            {
                convertToMonoDirty = true;
                return;
            }

            UpdateMonoChange(value);
        }
    }

    public AudioStream? AudioStream { get; set; }

    private AudioStreamReader? audioReader;

    private AudioSource? audioSource;
    private ISampleProvider? monoResampler;
    private bool convertToMono;
    private bool convertToMonoDirty;
    private WaveBuffer? waveBuffer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.05f;
    }

    private void Start()
    {
        OnEnable();
    }

    private void UpdateAudioClip()
    {
        if (audioReader == null)
        {
            throw new InvalidOperationException("AudioReader is not set");
        }

        if (audioSource == null)
        {
            throw new InvalidOperationException("AudioSource is not set");
        }

        var audioClip = AudioClip.Create("StreamedAudio", 4096, ConvertToMono ? 1 : 2, audioReader.WaveFormat.SampleRate, stream: true, PCMReaderCallback);
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    private void PCMReaderCallback(float[] floatData)
    {
        if (AudioStream == null || !AudioStream.Started)
            return;

        if (audioReader == null)
            return;

        if (waveBuffer == null || waveBuffer.MaxSize < floatData.Length * sizeof(float))
        {
            waveBuffer = new WaveBuffer(floatData.Length * sizeof(float));
        }

        int bytesRead;

        if (ConvertToMono && monoResampler != null)
        {
            bytesRead = monoResampler.Read(waveBuffer.FloatBuffer, 0, floatData.Length) * sizeof(float);
        }
        else
        {
            bytesRead = audioReader.Read(waveBuffer.ByteBuffer, 0, floatData.Length * sizeof(float));
        }

        Buffer.BlockCopy(waveBuffer.ByteBuffer, 0, floatData, 0, bytesRead);
    }

    private void OnEnable()
    {
        if (AudioStream == null)
            return;

        if (!AudioStream.Started)
            AudioStream.Start();

        audioReader = AudioStream.CreateReader();

        convertToMonoDirty = true;
        UpdateMonoChange(ConvertToMono);

        audioSource?.Play();
    }

    private void OnDisable()
    {
        audioSource?.Stop();
        audioReader?.Dispose();
        audioReader = null;

        convertToMonoDirty = true;
        UpdateMonoChange(false);

        if (AudioStream != null && AudioStream.NumReaders == 0)
        {
            AudioStream.Stop();
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private bool UpdateMonoChange(bool value)
    {
        if (!convertToMonoDirty)
            return false;

        convertToMonoDirty = false;

        if (value)
        {
            // Dispose old resampler if it exists
            if (monoResampler is IDisposable disposable)
            {
                Plugin.Logger.LogInfo("Disposing of old mono resampler");
                disposable.Dispose();
            }

            Plugin.Logger.LogInfo("Enabling mono resampler");
            monoResampler = new StereoToMonoSampleProvider(audioReader);
        }
        else
        {
            if (monoResampler is IDisposable disposable)
                disposable.Dispose();
            monoResampler = null;
        }

        if (audioSource != null && audioReader != null)
            UpdateAudioClip();

        return true;
    }

    public void Dispose()
    {
        if (monoResampler is IDisposable disposable)
            disposable.Dispose();

        audioReader?.Dispose();
    }
}
