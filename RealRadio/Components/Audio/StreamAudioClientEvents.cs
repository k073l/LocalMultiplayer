using UnityEngine;
using UnityEngine.Events;

namespace RealRadio.Components.Audio
{
    [RequireComponent(typeof(StreamAudioClient))]
    public class StreamAudioClientEvents : MonoBehaviour
    {
        public UnityEvent? OnHostStreamStarted;
        public UnityEvent? OnHostStreamStopped;
        public UnityEvent? OnHostStartRequested;

        private void Awake()
        {
            var client = GetComponent<StreamAudioClient>();

            client.OnHostStreamStarted += () => OnHostStreamStarted?.Invoke();
            client.OnHostStreamStopped += () => OnHostStreamStopped?.Invoke();
            client.OnHostStartRequested += () => OnHostStartRequested?.Invoke();
        }
    }
}
