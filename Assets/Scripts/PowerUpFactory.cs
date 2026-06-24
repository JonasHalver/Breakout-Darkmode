using UnityEngine;
using UnityEngine.Pool;

public class PowerUpFactory : MonoBehaviour
{
    public static PowerUpFactory Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] PowerUp _powerupPrefab;
    [SerializeField] Transform _poolRoot;

    [Header("Pool")]
    [SerializeField, Min(1)] int _defaultCapacity = 1;
    [SerializeField, Min(1)] int _maxPoolSize = 1;

    ObjectPool<PowerUp> _pool;
    PowerUp _activePowerup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_poolRoot == null)
        {
            _poolRoot = transform;
        }

        _pool = new ObjectPool<PowerUp>(
            CreatePowerUp,
            OnTakeFromPool,
            OnReturnToPool,
            OnDestroyPooledPowerUp,
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxPoolSize
        );
    }

    void OnDestroy()
    {
        _pool?.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static bool TrySpawn(PowerUpSpawnRequest request, out PowerUp powerup)
    {
        powerup = null;

        if (Instance == null)
        {
            Debug.LogError("No PowerupFactory exists in the scene.");
            return false;
        }

        return Instance.TrySpawnInternal(request, out powerup);
    }

    bool TrySpawnInternal(PowerUpSpawnRequest request, out PowerUp powerup)
    {
        powerup = null;

        if (_activePowerup != null)
        {
            return false;
        }

        if (!ConfigRepository.TryGetConfig(request.ConfigKey, out PowerUpConfigSO config))
        {
            Debug.LogError(
                $"Could not find PowerUpConfigSO '{request.ConfigKey}'."
            );

            return false;
        }

        powerup = _pool.Get();
        _activePowerup = powerup;

        powerup.Initialize(config, request, Release);

        return true;
    }

    PowerUp CreatePowerUp()
    {
        PowerUp powerUp = Instantiate(_powerupPrefab, _poolRoot);

        powerUp.gameObject.SetActive(false);

        return powerUp;
    }

    void OnTakeFromPool(PowerUp powerup)
    {
        powerup.gameObject.SetActive(true);
    }

    void OnReturnToPool(PowerUp powerUp)
    {
        if (_activePowerup == powerUp)
        {
            _activePowerup = null;
        }

        powerUp.ResetForPool();
        powerUp.transform.SetParent(_poolRoot, false);
        powerUp.gameObject.SetActive(false);
    }

    void OnDestroyPooledPowerUp(PowerUp powerUp)
    {
        Destroy(powerUp.gameObject);
    }

    void Release(PowerUp powerup)
    {
        _pool.Release(powerup);
    }
}
public struct PowerUpSpawnRequest
{
    public string ConfigKey;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 InitialVelocity;

    public PowerUpSpawnRequest(
        string configKey,
        Vector3 position,
        Quaternion rotation,
        Vector3 initialVelocity)
    {
        ConfigKey = configKey;
        Position = position;
        Rotation = rotation;
        InitialVelocity = initialVelocity;
    }
}