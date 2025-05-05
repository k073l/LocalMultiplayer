using System;
using UnityEngine;

namespace RealRadio.Data;

[Serializable]
[CreateAssetMenu(fileName = "RadioStation", menuName = "RealRadio/ScriptableObjects/RadioStation")]
public class RadioStation : ScriptableObject
{
    public string? Name;

    /// <summary>
    /// The abbreviation to use for this radio station. Used in the radial menu if there is no icon.
    /// </summary>
    public string? Abbreviation;

    public RadioType Type;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="RadioType.InternetRadio"/>, this is the url of the internet radio.
    /// </summary>
    public string? Url;

    /// <summary>
    /// Whether or not this radio station can be played by NPCs. This applies for vehicles driven by NPCs and random houses around the map.
    /// </summary>
    public bool CanBePlayedByNPCs = true;

    /// <summary>
    /// The icon to use for this radio station. Used in the radial menu. Should be a square texture, preferably minimum 128x128.
    /// </summary>
    public Sprite? Icon;

    /// <summary>
    /// An optional background color to use for this radio station. The background is used in the radial menu.
    /// </summary>
    public Color BackgroundColor = Color.clear;

    /// <summary>
    /// Whether or not to make the background rounded. Used in the radial menu.
    /// </summary>
    public bool RoundedBackground = true;

    /// <summary>
    /// Text color to use for the abbreviation text. Used in the radial menu.
    /// </summary>
    public Color TextColor = Color.white;

    public override string ToString()
    {
        return $"{Name} ({Type}): {TypeDataToString()}";
    }

    private string TypeDataToString()
    {
        return Type switch
        {
            RadioType.InternetRadio => Url ?? string.Empty,
            _ => throw new NotImplementedException($"Unknown type: {Type}"),
        };
    }
}
