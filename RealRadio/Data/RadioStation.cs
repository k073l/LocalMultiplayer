using System;
using UnityEngine;

namespace RealRadio.Data;

[Serializable]
[CreateAssetMenu(fileName = "RadioStation", menuName = "RealRadio/ScriptableObjects/RadioStation")]
public class RadioStation : ScriptableObject
{
    public string? Name;
    public RadioType Type;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="RadioType.InternetRadio"/>, this is the url of the internet radio.
    /// </summary>
    public string? Url;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="RadioType.LocalDirectory"/>, this is the path to the folder containing the audio files.
    /// </summary>
    public string? DirectoryPath;

    public override string ToString()
    {
        return $"{Name} ({Type}): {(Type == RadioType.InternetRadio ? Url : DirectoryPath)}";
    }
}
