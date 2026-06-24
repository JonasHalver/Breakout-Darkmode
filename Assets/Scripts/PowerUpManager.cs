using System;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    [SerializeField] List<string> _powerUpTypes = new();
    [SerializeField] List<int> _powerUpTimings = new();
    [SerializeField] BrickEventChannelSO _onBrickBreak;
    [SerializeField] PowerUpEventChannelSO _onPowerUpPickupTimeOut;
    [SerializeField] PowerUpEventChannelSO _onPowerUpPickUp;
    [SerializeField] PowerUpEventChannelSO _onPowerUpActivated;
    [SerializeField] PowerUpEventChannelSO _onPowerUpExpired;
    int _bricksBroken;
    bool _powerUpPickupActive;
    bool _powerUpActive;
    float _powerUpLifetime;
    Ball _ball;
    PowerUp _activePowerUp;
    string _lastPowerUpType;

    void Awake()
    {
        _ball = FindAnyObjectByType<Ball>();
    }

    void OnEnable()
    {
        _onBrickBreak.RegisterListener(BrickBroken);
        _onPowerUpPickUp.RegisterListener(ActivatePowerUp);
        _onPowerUpPickupTimeOut.RegisterListener(_ => _powerUpPickupActive = false);
    }
    void OnDisable()
    {
        _onBrickBreak.UnregisterListener(BrickBroken);
        _onPowerUpPickUp.UnregisterListener(ActivatePowerUp);
        _onPowerUpPickupTimeOut.UnregisterListener(_ => _powerUpPickupActive = false);
    }

    void Update()
    {
        if (_powerUpActive && _powerUpLifetime < Time.time)
        {
            _powerUpActive = false;
            _onPowerUpExpired.Raise(_activePowerUp);
            _activePowerUp = null;
        }
    }

    void ActivatePowerUp(PowerUp powerUp)
    {
        _powerUpActive = true;
        _powerUpLifetime = powerUp.Config.PowerUpLifetime + Time.time;
        _activePowerUp = powerUp;
        _onPowerUpActivated.Raise(powerUp);
    }

    void BrickBroken(Brick brick)
    {
        if (_powerUpPickupActive || _powerUpActive)
        {
            return;
        }
        _bricksBroken++;
        if (_powerUpTimings.Contains(_bricksBroken))
        {
            SpawnPowerUp(brick);
        }
    }

    void SpawnPowerUp(Brick brick)
    {
        var powerUpType = PickPowerUp();
        var pos = brick.transform.position;
        var brickRb = brick.GetComponent<Rigidbody>();
        var initialVelocity = brickRb.linearVelocity;
        var request = new PowerUpSpawnRequest
        {
            ConfigKey = powerUpType,
            Position = pos,
            Rotation = default,
            InitialVelocity = initialVelocity
        };
        PowerUpFactory.TrySpawn(request, out var powerup);
    }

    string PickPowerUp()
    {
        var tempList = new List<string>(_powerUpTypes);
        if (_lastPowerUpType != string.Empty)
        {
            tempList.Remove(_lastPowerUpType);
        }
        var index = UnityEngine.Random.Range(0, tempList.Count);
        var powerUpType = tempList[index];
        _lastPowerUpType = powerUpType;
        return powerUpType;
    }
}
