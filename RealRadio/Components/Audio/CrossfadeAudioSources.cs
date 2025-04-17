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

            MainSource.volume = (1 - Blend) * Mathf.Clamp(Volume, 0, 1);
            CrossfadeSource.volume = Blend * Mathf.Clamp(Volume, 0, 1);

            if (CrossfadeSource.volume > 0 && !CrossfadeSource.isPlaying)
                CrossfadeSource.Play();
            else if (CrossfadeSource.volume == 0 && CrossfadeSource.isPlaying)
                CrossfadeSource.Stop();
        }
    }
}
