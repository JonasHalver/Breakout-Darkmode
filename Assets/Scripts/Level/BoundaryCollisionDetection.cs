using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BoundaryCollisionDetection : MonoBehaviour
{
    const int MaxHits = 16;

    static readonly int BoundaryHitsId = Shader.PropertyToID("_BoundaryHits");
    static readonly int BoundaryHitCountId = Shader.PropertyToID("_BoundaryHitCount");

    [Header("Ring Lifetime")]
    [Tooltip("Should match, or slightly exceed, the RingDuration used by the shader.")]
    [SerializeField] float _hitLifetime = 0.7f;

    readonly Vector4[] _hits = new Vector4[MaxHits];

    MaterialPropertyBlock _propertyBlock;
    Renderer _renderer;

    int _hitCount;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();

        ClearAllHits();
        PushProperties();
    }

    void Update()
    {
        if (RemoveExpiredHits())
        {
            PushProperties();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount <= 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        RegisterHit(contact.point);
    }

    void RegisterHit(Vector3 worldPoint)
    {
        RemoveExpiredHits();

        if (_hitCount >= MaxHits)
        {
            RemoveOldestHit();
        }

        _hits[_hitCount] = new Vector4(
            worldPoint.x,
            worldPoint.y,
            worldPoint.z,
            Time.time
        );

        _hitCount++;

        PushProperties();
    }

    bool RemoveExpiredHits()
    {
        bool removedAny = false;

        for (int i = _hitCount - 1; i >= 0; i--)
        {
            float hitTime = _hits[i].w;
            float age = Time.time - hitTime;

            if (age > _hitLifetime)
            {
                RemoveHitAt(i);
                removedAny = true;
            }
        }

        return removedAny;
    }

    void RemoveOldestHit()
    {
        if (_hitCount <= 0)
        {
            return;
        }

        int oldestIndex = 0;
        float oldestTime = _hits[0].w;

        for (int i = 1; i < _hitCount; i++)
        {
            if (_hits[i].w < oldestTime)
            {
                oldestTime = _hits[i].w;
                oldestIndex = i;
            }
        }

        RemoveHitAt(oldestIndex);
    }

    void RemoveHitAt(int index)
    {
        if (index < 0 || index >= _hitCount)
        {
            return;
        }

        for (int i = index; i < _hitCount - 1; i++)
        {
            _hits[i] = _hits[i + 1];
        }

        _hitCount--;

        _hits[_hitCount] = new Vector4(0f, 0f, 0f, -9999f);
    }

    void ClearAllHits()
    {
        _hitCount = 0;

        for (int i = 0; i < _hits.Length; i++)
        {
            _hits[i] = new Vector4(0f, 0f, 0f, -9999f);
        }
    }

    void PushProperties()
    {
        if (_renderer == null)
        {
            return;
        }

        _renderer.GetPropertyBlock(_propertyBlock);

        _propertyBlock.SetVectorArray(BoundaryHitsId, _hits);
        _propertyBlock.SetFloat(BoundaryHitCountId, _hitCount);

        _renderer.SetPropertyBlock(_propertyBlock);
    }
}