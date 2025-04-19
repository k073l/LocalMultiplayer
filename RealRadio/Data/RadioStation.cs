using System;
using System.IO;

namespace RealRadio.Data;

[Serializable]
public struct RadioStation
{
    public string Name;
    public RadioType Type;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="RadioType.InternetRadio"/>, this is the url of the internet radio.
    /// </summary>
    public string? Url;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="RadioType.LocalDirectory"/>, this is the path to the folder containing the audio files.
    /// </summary>
    public string? DirectoryPath;
}
