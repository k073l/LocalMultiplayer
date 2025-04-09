using UnityEngine;
using UnityEditor;
using System.IO;

public class ExportAssetBundle : MonoBehaviour
{
    private const string OutputDirectory = "../AssetBundles/";

    [MenuItem("RealRadio/Export Asset Bundle")]
    public static void Export()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Debug.Log("Creating directory: " + Path.GetFullPath(OutputDirectory));
            Directory.CreateDirectory(OutputDirectory);
        }

        Debug.Log($"Using build settings: {EditorUserBuildSettings.activeBuildTarget}");

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(OutputDirectory, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

        if (manifest != null)
        {
            Debug.Log("Asset Bundle Exported!");

            foreach (var bundle in manifest.GetAllAssetBundles())
            {
                Debug.Log($"- {Path.Combine(Path.GetFullPath(OutputDirectory), bundle)}");
            }
        }
    }
}
