using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Experimental.Audio;

namespace RealRadio.Components
{
    [RequireComponent(typeof(AudioSource))]
    public class StreamAudioClient : MonoBehaviour
    {
        public bool ConvertToMono { get; set; }

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
            }
        }

        public int Id { get; set; }

        private StreamAudioHost? host;
        private bool initialized;
        private AudioSource audioSource = null!;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>() ?? throw new InvalidOperationException("No AudioSource component found on game object");
            audioSource.priority = 1;

            int numChannels = 2;
            audioSource.clip = AudioClip.Create("Dummy", 2048, numChannels, AudioSettings.GetSampleRate(), false);
            audioSource.loop = true;
            float[] audioData = new float[2048 * numChannels];
            Array.Fill(audioData, 1);
            audioSource.clip.SetData(audioData, 0);

            // Set some sane defaults on the audio source
            audioSource.volume = 0.1f;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CreateLogarithmicRolloffCurve(maxDistance: 30f));
            audioSource.maxDistance = 30f;
            audioSource.dopplerLevel = 0.25f;
        }

        private AnimationCurve CreateLogarithmicRolloffCurve(float maxDistance)
        {
            var keyFrames = new Keyframe[(int)Math.Floor(maxDistance) * 2 + 1];

            float distancePerKeyframe = maxDistance / (keyFrames.Length - 1);

            for (int i = 0; i < keyFrames.Length; ++i)
            {
                float distance = i * distancePerKeyframe;
                //float value = 1f / distance;
                float value = 1f - Mathf.Log10(distance) / Mathf.Log10(maxDistance);
                value = Math.Clamp(value, 0f, 1f); // save my ears from potential ruin
                keyFrames[i] = new Keyframe(distance, value);
            }

            return new AnimationCurve(keyFrames);
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

            audioSource.Play();
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
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (host == null || host.AudioData == null || !host.enabled || host.AudioStream?.StreamAvailable != true)
            {
                Array.Fill(data, 0);
                return;
            }

            if (!ConvertToMono)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] *= host.AudioData[i];
                }
            }
            else
            {
                for (int i = 0; i < data.Length / channels; ++i)
                {
                    float average = 0;

                    for (int j = 0; j < channels; ++j)
                    {
                        average += host.AudioData[i * channels + j];
                    }

                    average /= channels;

                    for (int j = 0; j < channels; ++j)
                    {
                        data[i * channels + j] *= average;
                    }
                }
            }
        }
    }
}
