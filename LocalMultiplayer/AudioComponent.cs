using UnityEngine;

namespace LocalMultiplayer;

public class AudioComponent : MonoBehaviour
{
    private void Update()
    {
        bool windowFocused = Application.isFocused;
        AudioListener.volume = windowFocused ? 1 : 0;
    }
}
