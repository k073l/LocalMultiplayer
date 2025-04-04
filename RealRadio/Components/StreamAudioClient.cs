using System;
using UnityEngine;
using UnityEngine.Experimental.Audio;

namespace RealRadio.Components
{
    [RequireComponent(typeof(AudioSource))]
    public class StreamAudioClient : MonoBehaviour
    {
        public StreamAudioHost? Host
        {
            get => host;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (host != null)
                    throw new InvalidOperationException("Host is already set");

                host = value;
                host.OnAudioClipChanged += OnHostAudioClipChanged;
            }
        }

        public int Id { get; set; }

        private StreamAudioHost? host;
        private bool initialized;
        private AudioClip? audioClip;
        private AudioSource audioSource = null!;
        private int audioDataPosition;
        private DateTime lastCallbackTime;
        private bool isFirstPcmCallback = true;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>() ?? throw new InvalidOperationException("No AudioSource component found on game object");
            audioSource.priority = 1;
        }

        private void Start()
        {
            if (!initialized)
            {
                initialized = true;
                OnEnable();
            }
        }

        private void OnEnable()
        {
            if (!initialized)
                return;

            if (host == null)
                throw new InvalidOperationException("Host is not set");

            if (host.AudioClip != null)
                OnHostAudioClipChanged(host.AudioClip);

            if (audioClip != null)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }

        private void OnDisable()
        {
            if (!initialized)
                return;

            audioSource.Stop();
        }

        private void OnDestroy()
        {
            Host?.DestroyClient(this);

            audioSource.clip = null;
            Destroy(audioClip);
        }

        private void OnHostAudioClipChanged(AudioClip clip)
        {
            Destroy(audioClip);

            Plugin.Logger.LogInfo("Test");
            audioClip = AudioClip.Create($"StreamedAudioClient{Id}", clip.samples, clip.channels, clip.frequency, stream: true, PCMReaderCallback);
            Plugin.Logger.LogInfo("Test2");
            isFirstPcmCallback = false;

            audioSource.clip = audioClip;

            if (enabled)
                audioSource.Play();
        }

        private void PCMReaderCallback(float[] data)
        {
            if ((DateTime.UtcNow - lastCallbackTime) > TimeSpan.FromMilliseconds(50))
            {
                audioDataPosition = 0;
            }

            lastCallbackTime = DateTime.UtcNow;

            if (host == null || host.AudioData == null || !host.enabled || isFirstPcmCallback)
            {
                Plugin.Logger.LogInfo($"client {Id}: Skipping callback");
                Array.Fill(data, 0);
                return;
            }

            int length = Math.Min(data.Length, host.AudioDataLength - audioDataPosition);

            //Plugin.Logger.LogInfo($"client {Id}: Copying {length} bytes from index {audioDataPosition}");

            host.AudioData.AsSpan(audioDataPosition, length).CopyTo(data.AsSpan(0, length));
            audioDataPosition += length;

            //Plugin.Logger.LogInfo($"client {Id}: Audio data position: {audioDataPosition} / {host.AudioDataLength}");

            if (length < data.Length)
            {
                Plugin.Logger.LogWarning($"Audio data is too short, audio will cut off (host len: {length}, data len: {data.Length})");
                Array.Fill(
                    array: data,
                    value: 0,
                    startIndex: length,
                    count: data.Length - length
                );
            }
        }
    }
}
