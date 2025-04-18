using ScheduleOne.Audio;
using UnityEngine;

namespace RealRadio.Components.Audio
{
    public class CrossfadeAudioSources : MonoBehaviour
    {
        [Range(0, 1)]
        public float Volume = 1f;

        [field: Range(0, 1)]
        [field: SerializeField]
        public float Blend { get; set; }

        public AudioSource? MainSource;
        public AudioSource? CrossfadeSource;

        private AudioSourceController? MainSourceController;
        private AudioSourceController? CrossfadeSourceController;

        private void Awake()
        {
            MainSourceController = MainSource?.GetComponent<AudioSourceController>();
            CrossfadeSourceController = CrossfadeSource?.GetComponent<AudioSourceController>();
        }

        private void Update()
        {
            if (MainSource == null || CrossfadeSource == null)
                return;

            if (!MainSource.isPlaying)
            {
                if (CrossfadeSource.isPlaying)
                    CrossfadeSource.Stop();

                return;
            }

            UpdateVolume(MainSource, MainSourceController, (1 - Blend) * Volume);
            UpdateVolume(CrossfadeSource, CrossfadeSourceController, Blend * Volume);

            if (CrossfadeSource.volume > 0 && !CrossfadeSource.isPlaying)
                CrossfadeSource.Play();
            else if (CrossfadeSource.volume == 0 && CrossfadeSource.isPlaying)
                CrossfadeSource.Stop();
        }

        private void UpdateVolume(AudioSource source, AudioSourceController? controller, float volume)
        {
            volume = Mathf.Clamp(volume, 0, 1);

            if (controller == null)
            {
                if (!Mathf.Approximately(source.volume, volume))
                    source.volume = volume;
            }
            else
            {
                if (!Mathf.Approximately(controller.Volume, volume))
                    controller.SetVolume(volume);
            }
        }
    }
}
