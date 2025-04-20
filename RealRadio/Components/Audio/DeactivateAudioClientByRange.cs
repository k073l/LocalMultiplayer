using System;
using UnityEngine;

namespace RealRadio.Components.Audio;

[RequireComponent(typeof(StreamAudioClient))]
[RequireComponent(typeof(AudioSource))]
public class DeactivateAudioClientByRange : MonoBehaviour
{
    private StreamAudioClient client = null!;
    private AudioSource audioSource = null!;
    private AudioListener? cachedListener;

    public bool IsInRange()
    {
        if (!CacheAudioListener() || cachedListener == null)
            return false;

        float maxDistance = audioSource.maxDistance + 5f;
        float distanceSqrd = (cachedListener.transform.position - transform.position).sqrMagnitude;

        return distanceSqrd <= maxDistance * maxDistance;
    }

    private void Awake()
    {
        client = GetComponent<StreamAudioClient>() ?? throw new InvalidOperationException("No StreamAudioClient component found on game object");
        audioSource = GetComponent<AudioSource>() ?? throw new InvalidOperationException("No AudioSource component found on game object");
    }

    private void Update()
    {
        bool isInRange = IsInRange();

        if (!isInRange && client.enabled)
        {
            Plugin.Logger.LogDebug("Deactivating audio client due to distance");
            client.enabled = false;
        }
        else if (isInRange && !client.enabled)
        {
            Plugin.Logger.LogDebug("Activating audio client due to distance");
            client.enabled = true;
        }
    }

    private bool CacheAudioListener()
    {
        if (!cachedListener)
            cachedListener = FindObjectOfType<AudioListener>();

        if (!cachedListener)
            return false;

        return true;
    }
}
