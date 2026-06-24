using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PowerUp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Renderer _renderer;
    [SerializeField] Rigidbody _rigidbody;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _audioClip;
    
    [Header("Events")]
    [SerializeField] PowerUpEventChannelSO _onTimeOut;
    [SerializeField] PowerUpEventChannelSO _onPickUp;

    PowerUpConfigSO _config;
    Action<PowerUp> _releaseToPool;

    float _despawnTime;
    bool _isActivePowerup;
    public PowerUpConfigSO Config => _config;

    void Awake()
    {
        if (_rigidbody == null)
        {
            TryGetComponent(out _rigidbody);
        }

        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<Renderer>();
        }
    }

    public void Initialize(PowerUpConfigSO config, PowerUpSpawnRequest request, Action<PowerUp> releaseToPool)
    {
        _config = config;
        _releaseToPool = releaseToPool;

        transform.SetPositionAndRotation(
            request.Position,
            request.Rotation
        );
        _renderer.material = config.Material;
        _rigidbody.linearVelocity = request.InitialVelocity;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.useGravity = config.GravityMultiplier > 0f;

        _despawnTime = Time.time + config.PickupLifetime;
        _isActivePowerup = true;
    }

    void Update()
    {
        if (!_isActivePowerup)
        {
            return;
        }

        if (Time.time >= _despawnTime)
        {
            _onTimeOut.Raise(this);
            ReturnToPool();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (!other.transform.CompareTag("Paddle"))
        {
            return;
        }
        _audioSource.PlayOneShot(_audioClip);
        _onPickUp.Raise(this);
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (!_isActivePowerup)
        {
            return;
        }

        _isActivePowerup = false;
        _releaseToPool?.Invoke(this);
    }

    public void ResetForPool()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        _config = null;
        _releaseToPool = null;
    }
}