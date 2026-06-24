using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSfxManager : MonoBehaviour
{
    [SerializeField] int _maxSoundsPerFrame = 4;
    [SerializeField] int _maxBrickSoundsPerFrame = 2;
    [SerializeField] SoundEffectEventChannelSO _onSoundTriggered;
    int _soundsPlayedThisFrame;
    int _brickSoundsPlayedThisFrame;
    int _lastProcessedFrame = -1;

    readonly Dictionary<int, float> _nextAllowedPlayTime = new();

    public struct SoundInfo
    {
        public AudioSource Source;
        public AudioClip Clip;
        public float Volume;
        public float Pitch;
        public float Cooldown;
        public bool IsBrickSound;
    }
    void OnEnable()
    {
        _onSoundTriggered.RegisterListener(TryPlay);
    }
    
    void OnDisable()
    {
        _onSoundTriggered.UnregisterListener(TryPlay);
    }

    public void TryPlay(SoundInfo info)
    {
        if (info.Source == null || info.Clip == null)
        {
            return;
        }

        ResetFrameBudgetIfNeeded();

        if (_soundsPlayedThisFrame >= _maxSoundsPerFrame)
        {
            return;
        }

        if (info.IsBrickSound && _brickSoundsPlayedThisFrame >= _maxBrickSoundsPerFrame)
        {
            return;
        }

        int sourceId = info.Source.GetInstanceID();

        if (_nextAllowedPlayTime.TryGetValue(sourceId, out float nextTime) &&
            Time.time < nextTime)
        {
            return;
        }

        info.Source.pitch = info.Pitch;
        info.Source.PlayOneShot(info.Clip, info.Volume);

        _nextAllowedPlayTime[sourceId] = Time.time + info.Cooldown;

        _soundsPlayedThisFrame++;

        if (info.IsBrickSound)
        {
            _brickSoundsPlayedThisFrame++;
        }
    }

    void ResetFrameBudgetIfNeeded()
    {
        if (_lastProcessedFrame == Time.frameCount)
        {
            return;
        }

        _lastProcessedFrame = Time.frameCount;
        _soundsPlayedThisFrame = 0;
        _brickSoundsPlayedThisFrame = 0;
    }
}