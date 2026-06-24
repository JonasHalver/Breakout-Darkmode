using System;
using UnityEngine;

public class RigidbodySpeedLimiter : MonoBehaviour
{
    Rigidbody _rigidbody;
    [SerializeField] float _maxSpeed = 10f;

    void Awake()
    {
        TryGetComponent(out _rigidbody);
    }
    void FixedUpdate()
    {
        _rigidbody.linearVelocity = Vector3.ClampMagnitude(_rigidbody.linearVelocity, _maxSpeed);
    }
}
