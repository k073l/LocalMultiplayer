using MelonLoader;
using UnityEngine;

namespace LocalMultiplayer;

[RegisterTypeInIl2Cpp]
public class AudioComponent : MonoBehaviour
{
    private void Update()
    {
        bool windowFocused = Application.isFocused;
        AudioListener.volume = windowFocused ? 1 : 0;
    }
}
