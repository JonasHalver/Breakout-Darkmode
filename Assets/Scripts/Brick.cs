using System;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    [SerializeField] BrickEventChannelSO _onBreak;
    BrickConfigSO _config;
    float _health;
    EmissiveMaterialHandler _emissiveMaterialHandler;
    Rigidbody _rigidbody;
    Renderer _renderer;
    Vector3 _basePosition;
    Quaternion _baseRotation;
    IBrickBreakEffect _breakEffect;
    IVFXSource _vfxSource;
    
    public void Initialize(BrickConfigSO config, string musicalNote)
    {
        _config = config;
        _health = config.MaxHealth;
        
        TryGetComponent(out _emissiveMaterialHandler);
        TryGetComponent(out _rigidbody);
        TryGetComponent(out _renderer);
        TryGetComponent(out _vfxSource);
        
        _rigidbody.useGravity = false;
        _rigidbody.mass = 1000;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezePositionZ;
        if (ConfigRepository.TryGetConfig("BrickBlink", out BlinkConfigSO blinkConfig))
        {
            _emissiveMaterialHandler.Config = blinkConfig;
        }
        else
        {
            Debug.LogError("Failed to initialize Brick. BrickBlink config was missing.");
        }
        _emissiveMaterialHandler.Initialize((_ => {}));
        
        if (TryGetComponent(out BoxCollider col))
        {
            col.material = _config.PhysicsMaterial;
        }

        if (TryGetComponent(out _breakEffect))
        {
            _breakEffect.Initialize(this, _vfxSource);
        }
        
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;
        _renderer.enabled = true; // Starts out disabled from factory
    }

    public void SetSize(Vector3 worldSize)
    {
        var baseScale = transform.localScale;
        var isQuarterTurn = IsQuarterTurn(transform.eulerAngles.z);

        var localSize = isQuarterTurn
            ? new Vector3(worldSize.y, worldSize.x, worldSize.z)
            : worldSize;

        transform.localScale = Vector3.Scale(baseScale, localSize);
    }

    static bool IsQuarterTurn(float zRotation)
    {
        var normalizedRotation = Mathf.Repeat(zRotation, 360f);

        return Mathf.Abs(Mathf.DeltaAngle(normalizedRotation, 90f)) < 0.1f ||
               Mathf.Abs(Mathf.DeltaAngle(normalizedRotation, 270f)) < 0.1f;
    }
    void Hit(float damage)
    {
        if (_config.IsIndestructible)
            return;

        _health -= damage; // do something cool with health lowering, not just breaking. Maybe make it more susceptible to physics.
        var percentageLost = (_config.MaxHealth - _health) / _config.MaxHealth;
        _emissiveMaterialHandler.IntensityRamp(percentageLost, true);
        if (_health <= 0)
        {
            Break();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        var force = other.impulse.magnitude;
        Hit(force);
    }

    void Break()
    {
        _onBreak.Raise(this);
        _breakEffect?.Break();
    }

    void ResetBrick()
    {
        transform.localPosition = _basePosition;
        transform.localRotation = _baseRotation;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _health = _config.MaxHealth;
        

        _breakEffect?.ResetBrick();
        // Do we need to reset the emissive material?
    }
}
