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

            if (value)
            {
                monoResampler = new StereoToMonoSampleProvider(AudioReader);
            }
            else
            {
                if (monoResampler is IDisposable disposable)
                    disposable.Dispose();
                monoResampler = null;
            }
        }
    }

    public AudioStreamReader? AudioReader { get; set; }

    private AudioSource? audioSource;
    private ISampleProvider? monoResampler;
    private AudioClip? audioClip;
    private bool convertToMono;
    private WaveBuffer? waveBuffer;

    private void Start()
    {
        if (AudioReader == null)
            throw new InvalidOperationException("AudioReader is not set");

        audioClip = AudioClip.Create("StreamedAudio", 4096, ConvertToMono ? 1 : 2, AudioReader.WaveFormat.SampleRate, stream: true, PCMReaderCallback);

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.volume = 0.05f;
        audioSource.playOnAwake = true;
    }

    private void PCMReaderCallback(float[] floatData)
    {
        if (AudioReader == null)
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
            bytesRead = AudioReader.Read(waveBuffer.ByteBuffer, 0, floatData.Length * sizeof(float));
        }

        Buffer.BlockCopy(waveBuffer.ByteBuffer, 0, floatData, 0, bytesRead);
    }

    private void OnEnable()
    {
        AudioReader?.ResetToEnd();
        audioSource?.Play();
    }

    private void OnDisable()
    {
        audioSource?.Stop();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (monoResampler is IDisposable disposable)
            disposable.Dispose();

        AudioReader?.Dispose();
    }
}
