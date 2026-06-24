using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SFXHandler : MonoBehaviour
{
    [SerializeField] SoundEffectEventChannelSO _onSoundTriggered;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _testClip;
    [SerializeField] bool _shiftPitch;
    [SerializeField] float _soundCooldown;
    [SerializeField] bool _isBrick;
    float _forceOfLastImpact;
    void PlaySfx(AudioClip clip)
    {
        var rand = Random.Range(0, NoteOffsetsFromEb3.Count);
        var targetNote = NoteOffsetsFromEb3.Keys.ElementAt(rand);
        var pitch = _shiftPitch ? PitchShift(targetNote) : 1;
        
        var soundInfo = new CollisionSfxManager.SoundInfo
        {
            Source = _audioSource,
            Clip = clip,
            Volume = Volume(),
            Pitch = pitch,
            Cooldown = _soundCooldown,
            IsBrickSound = _isBrick
        };
        _onSoundTriggered?.Raise(soundInfo);
        //_audioSource.PlayOneShot(clip);
    }

    void OnCollisionEnter(Collision other)
    {
        _forceOfLastImpact = other.impulse.magnitude;
        PlaySfx(_testClip);
    }

    float PitchShift(string targetNote)
    {
        var semitones = NoteOffsetsFromEb3[targetNote];
        return Mathf.Pow(2, semitones / 12.0f);
    }

    float Volume()
    {
        var volume = Mathf.InverseLerp(100, 400, _forceOfLastImpact);
        volume = Mathf.Clamp01(volume);
        volume = Mathf.Max(volume, 0.1f);
        return volume;
    }
    static readonly Dictionary<string, int> NoteOffsetsFromEb3 = new() //Eb3 is the note of the SFX I found
    {
        { "Eb", 0 },
        { "E", 1 },
        { "F", 2 },
        { "F#", 3 },
        { "Gb", 3 },
        { "G", 4 },
        { "G#", 5 },
        { "Ab", 5 },
        { "A", 6 },
        { "A#", 7 },
        { "Bb", 7 },
        { "B", 8 },
        { "C", 9 },
        { "C#", 10 },
        { "Db", 10 },
        { "D", 11 },
        { "D#", 12 },
        { "Eb4", 12 }
    };
}
