using System;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] Rigidbody _rigidbody;
    [SerializeField] float _maxSpeed;
    EmissiveMaterialHandler _emissiveMaterialHandler;
    [SerializeField] int _hitsUntilSplit = 10;
    int _hits;
    static int _ballCount = 1;
    static List<Ball> _spawnedBalls = new();
    static Ball _original;
    Vector3 _lastValidDirection;
    TrailRenderer _trail;
    bool _attached = true;
    Vector3 _ballVelocity;
    float _speedRateLastFrame;
    float _baseMass;
    Vector3 _baseScale;
    float _currentMaxSpeed;
    bool _allowSplits;
    [Header("Events")] 
    [SerializeField] PowerUpEventChannelSO _onPowerUpActivated;
    [SerializeField] PowerUpEventChannelSO _onPowerUpExpired;
    float _lastYPosition;
    float _stuckTime;
    public static bool BallIsStuck;
    public Vector3 BallVelocity
    {
        get
        {
            if (_attached)
            {
                return _ballVelocity;
            }

            return _rigidbody.linearVelocity;
        }
        set => _ballVelocity = value;
    }

    public bool Attached
    {
        get => _attached;
        set
        {
            _attached = value;
            if (value)
            {
                _trail.enabled = false;
                _trail.Clear();
            }
            else
            {
                _trail.enabled = true;
            }
        }
    }

    void Awake()
    {
        TryGetComponent(out _emissiveMaterialHandler);
        BlinkConfigSO config;
        if (ConfigRepository.TryGetConfig("BallBlink", out config))
        {
            _emissiveMaterialHandler.Config = config;
        }
        else
        {
            Debug.LogError("Failed to initialize Ball. BallBlink config was missing.");
        }
        _emissiveMaterialHandler.Initialize((success => Debug.Log($"BlinkHandler on {gameObject.name} initialized: {success}")), false);
        _trail = GetComponent<TrailRenderer>();
        _trail.enabled = false;
        _currentMaxSpeed = _maxSpeed;
        _baseMass = _rigidbody.mass;
        _baseScale = transform.localScale;
    }

    void OnEnable()
    {
        _onPowerUpActivated.RegisterListener(ApplyPowerUp);
        _onPowerUpExpired.RegisterListener(ResetPowerUps);
    }
    void OnDisable()
    {
        _onPowerUpActivated.UnregisterListener(ApplyPowerUp);
        _onPowerUpExpired.UnregisterListener(ResetPowerUps);
    }

    void FixedUpdate()
    {
        var velocity = BallVelocity;
        var currentSpeed = velocity.magnitude;
        if (currentSpeed > 0.01f)
        {
            _lastValidDirection = velocity.normalized;
        }

        var speedCorrectionRate = currentSpeed < _currentMaxSpeed ? 10 : 0.5f;
        var correctedSpeed = Mathf.MoveTowards(currentSpeed, _currentMaxSpeed, Time.fixedDeltaTime * speedCorrectionRate);
        var direction = currentSpeed > 0.01f ? _lastValidDirection : velocity.normalized;
        _rigidbody.linearVelocity = direction * correctedSpeed;

        float speedRate;
        if (currentSpeed <= 0)
        {
            speedRate = 0;
        }
        else
        {
            speedRate = currentSpeed / _currentMaxSpeed;
        }
        speedRate = Mathf.Clamp01(speedRate);
        var decayRate = speedRate < _speedRateLastFrame ? 0.5f : 10f;
        speedRate = Mathf.Lerp(_speedRateLastFrame, speedRate, Time.fixedDeltaTime * decayRate);
        _emissiveMaterialHandler.IntensityRamp(speedRate);
        _speedRateLastFrame = speedRate;

        _trail.startWidth = transform.localScale.x;
        CheckBallStuckStatus();
    }

    void CheckBallStuckStatus()
    {
        if (Attached)
        {
            BallIsStuck = false;
            return;
        }
        if (Mathf.Approximately(transform.position.y, _lastYPosition))
        {
            _stuckTime += Time.fixedDeltaTime;
            if (_stuckTime >= 1.0f)
            {
                BallIsStuck = true;
            }
        }
        else
        {
            _stuckTime = 0.0f;
            BallIsStuck = false;
        }

        _lastYPosition = transform.position.y;
    }

    void OnCollisionEnter(Collision other)
    {
        if (!_allowSplits)
        {
            return;
        }
        _hits++;
        _emissiveMaterialHandler.IntensityRamp(_hits / (float)_hitsUntilSplit);
        if (_hits >= _hitsUntilSplit)
        {
            Split();
        }
    }

    void ApplyPowerUp(PowerUp powerUp)
    {
        switch (powerUp.Config.Type)
        {
            case PowerUpType.IncreaseMass:
                IncreaseMass(100.0f);
                break;
            case PowerUpType.SplitBall:
                AllowSplits(true);
                break;
            case PowerUpType.FastBall:
                EnableFastBall();
                break;
            case PowerUpType.MultiBall:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    void AllowSplits(bool allow)
    {
        _allowSplits = allow;
        Split();
    }
    void IncreaseMass(float newMass)
    {
        _rigidbody.mass = newMass;
        transform.localScale = Vector3.one;
    }
    void ResetMass()
    {
        _rigidbody.mass = _baseMass;
        transform.localScale = _baseScale;
    }
    void EnableFastBall()
    {
        _currentMaxSpeed = _maxSpeed * 10;
        transform.localScale = _baseScale * 0.5f;
    }
    void ResetSpeed()
    {
        _currentMaxSpeed = _maxSpeed;
        transform.localScale = _baseScale;
    }
    void ResetPowerUps(PowerUp powerUp)
    {
        ResetMass();
        ResetSpeed();
        AllowSplits(false);
        Reset();
    }
    void Split()
    {
        _hits = 0;
        _emissiveMaterialHandler.EndIntensityRamp();
        if (_ballCount == 1)
        {
            _original = this;
        }
        if (_ballCount >= 20)
        {
            return;
        }
        var newBall = Instantiate(gameObject, transform.position, Quaternion.identity);
        var script = newBall.GetComponent<Ball>();
        script.Attached = false;
        var newRigidbody = newBall.GetComponent<Rigidbody>();
        newRigidbody.linearVelocity = -_rigidbody.linearVelocity;
        var newScale = transform.localScale * 0.9f;
        newBall.transform.localScale = newScale;
        transform.localScale = newScale;
        _ballCount++;
        _spawnedBalls.Add(script);
    }

    public void Reset()
    {
        Unsplit();
    }
    void Unsplit()
    {
        if (this != _original)
        {
            return;
        }
        _ballCount = 1;
        for (int i = 0; i < _spawnedBalls.Count; i++)
        {
            var ball = _spawnedBalls[i];
            Destroy(ball.gameObject);
            _spawnedBalls.RemoveAt(i);
            i--;
        }
        transform.localScale = _baseScale;
        _hits = 0;
    }
}
