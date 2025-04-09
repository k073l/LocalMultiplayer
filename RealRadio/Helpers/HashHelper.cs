using System;
using UnityEngine;

namespace RealRadio.Helpers;

public static class HashHelper
{
    public static ushort Get16BitHash(this AssetBundle assetBundle)
    {
        if (assetBundle == null)
            throw new ArgumentNullException(nameof(assetBundle));

        int hash = 0;

        unchecked
        {
            foreach (string assetName in assetBundle.GetAllAssetNames())
            {
                hash *= 31 * assetName.GetHashCode();
            }

            hash *= 31 * assetBundle.name.GetHashCode();
        }

        return (ushort)(ushort.MaxValue - (ushort)(hash % ushort.MaxValue));
    }
}
