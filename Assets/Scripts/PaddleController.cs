using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PaddleController : MonoBehaviour
{
    Rigidbody _rigidbody;
    [Header("Input")]
    [SerializeField] Camera _targetCamera;
    [SerializeField] InputAction _bounceAction;
    [SerializeField] InputAction _resetAction;

    [Header("Movement")]
    [SerializeField] bool _lockYPosition = true;
    [SerializeField] float _moveSpeed = 40f;
    [SerializeField] bool _snapToMouse = true;

    [Header("Bounds")]
    [SerializeField] float _edgePadding;

    [SerializeField, Tooltip("Percentage of screen height from bottom"), Range(0.0f,1.0f)] float _lockedY = 0.1f;
    Collider[] _childColliders;

    [Header("Ball")] 
    [SerializeField] Ball _ball;
    [SerializeField] Rigidbody _ballRigidbody;
    [SerializeField] Vector3 _ballOffsetWhileAttached;
    
    [Header("Bounce")]
    [SerializeField] AnimationCurve _bounceCurve;
    [SerializeField] float _minBounceHeight = 0.35f;
    [SerializeField] float _maxBounceHeight = 0.35f;
    [SerializeField] float _bounceDuration = 0.18f;
    
    [SerializeField] float _maxHoldTime = 2.0f;
    
    [Header("Physics Movement")]
    [SerializeField] float _maxSnapSpeed = 80f;

    [Header("Graphics")] 
    [SerializeField] string _blinkConfigName;

    bool _bounceQueued;
    bool _isBouncing;
    float _bounceTimer;
    float _previousBounceOffset;
    float _currentBounceVelocityY;
    bool _ballIsAttached = true;
    bool _resetQueued;
    bool _held;
    float _holdStartedTime;
    float _bounceInvLerp;
    EmissiveMaterialHandler _emissiveMaterialHandler;
    float _holdRate;
    
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _ball = FindAnyObjectByType<Ball>();
        _ballRigidbody = _ball?.GetComponent<Rigidbody>();
        if (_targetCamera == null)
            _targetCamera = Camera.main;
        RefreshBoundsSources();
        _bounceAction = InputSystem.actions.FindAction("Attack"); // Attack is placeholder
        _bounceAction.performed += ctx =>
        {
            _held = true;
            _holdStartedTime = Time.time;
        };
        _bounceAction.canceled += ctx => _bounceQueued = true;
        _resetAction = InputSystem.actions.FindAction("Jump"); // Jump is placeholder
        if (TryGetComponent(out _emissiveMaterialHandler))
        {
            if (!ConfigRepository.TryGetConfig(_blinkConfigName, out BlinkConfigSO config))
            {
                Debug.LogError("Failed to initialize PaddleController. BlinkConfig was missing.");
            }
            _emissiveMaterialHandler.Config = config;
            _emissiveMaterialHandler.Initialize((success => Debug.Log($"BlinkHandler on {gameObject.name} initialized: {success}")));
        }
    }

    void Update()
    {
        if (_resetAction.triggered)
        {
            _ball.Attached = true;
            _resetQueued = true;
        }

        if (_held)
        {
            var holdTime = Mathf.Clamp(Time.time - _holdStartedTime, 0f, _maxHoldTime);
            _holdRate = holdTime / _maxHoldTime;
            _emissiveMaterialHandler.IntensityRamp(_holdRate);
        }
        else
        {
            var holdDecay = _holdRate -= Time.deltaTime * 0.5f;
            holdDecay = Mathf.Clamp01(holdDecay);
            _emissiveMaterialHandler.IntensityRamp(holdDecay);
        }
    }

    void FixedUpdate()
    {
        if (_targetCamera == null)
        {
            return;
        }

        if (!TryGetMouseWorldPosition(out Vector3 mouseWorldPosition))
        {
            return;
        }

        bool bouncePressedThisFrame = _bounceQueued;
        _bounceQueued = false;

        if (bouncePressedThisFrame)
        {
            StartBounce();
        }
        
        var resetPressedThisFrame = _resetQueued;
        _resetQueued = false;
        if (resetPressedThisFrame)
        {
            _ball.Reset();
            _ballIsAttached = true;
        }

        float bounceOffset = EvaluateBounceOffset();

        Vector3 targetPosition = _rigidbody.position;

        targetPosition.x = ClampPaddlePivotX(mouseWorldPosition.x);

        if (_lockYPosition)
        {
            targetPosition.y = FindPaddleHeight() + bounceOffset;
        }
        else
        {
            targetPosition.y += bounceOffset;
        }

        Vector3 paddleVelocity = CalculateVelocityToTarget(targetPosition);

        _rigidbody.linearVelocity = paddleVelocity;

        if (_ballIsAttached)
        {
            if (bouncePressedThisFrame)
            {
                _ballIsAttached = false;
            }
            else
            {
                HoldBallAtPaddle(targetPosition);
            }
        }
    }

    Vector3 CalculateVelocityToTarget(Vector3 targetPosition)
    {
        Vector3 difference = targetPosition - _rigidbody.position;

        Vector3 velocity = difference / Time.fixedDeltaTime;

        if (_snapToMouse)
        {
            velocity.x = Mathf.Clamp(velocity.x, -_maxSnapSpeed, _maxSnapSpeed);
        }
        else
        {
            velocity.x = Mathf.Clamp(velocity.x, -_moveSpeed, _moveSpeed);
        }

        velocity.z = 0f;

        return velocity;
    }

    void StartBounce()
    {
        _held = false;
        _ball.Attached = false;
        _emissiveMaterialHandler.EndIntensityRamp();
        _isBouncing = true;
        var heldTime = Mathf.Clamp(Time.time - _holdStartedTime, 0f, _maxHoldTime);
        _bounceInvLerp =  Mathf.InverseLerp(0, _maxHoldTime, heldTime);
        _bounceTimer = 0f;
        _previousBounceOffset = 0f;
        _currentBounceVelocityY = 0f;
    }

    float EvaluateBounceOffset()
    {
        if (!_isBouncing)
        {
            _currentBounceVelocityY = 0f;
            return 0f;
        }

        float normalizedTime = Mathf.Clamp01(_bounceTimer / _bounceDuration);
        var bounceHeight = Mathf.Lerp(_minBounceHeight, _maxBounceHeight, _bounceInvLerp);
        float currentOffset = _bounceCurve.Evaluate(normalizedTime) * bounceHeight;

        _currentBounceVelocityY =
            (currentOffset - _previousBounceOffset) / Time.fixedDeltaTime;

        _previousBounceOffset = currentOffset;

        _bounceTimer += Time.fixedDeltaTime;

        if (_bounceTimer >= _bounceDuration)
        {
            _isBouncing = false;
            _previousBounceOffset = 0f;
        }

        return currentOffset;
    }

    void HoldBallAtPaddle(Vector3 paddleTargetPosition)
    {
        if (_ballRigidbody == null)
        {
            return;
        }

        _ballRigidbody.linearVelocity = Vector3.zero;
        _ball.BallVelocity = _rigidbody.linearVelocity;
        _ballRigidbody.MovePosition(paddleTargetPosition + _ballOffsetWhileAttached);
    }

    float FindPaddleHeight()
    {
        var playfield = DynamicPlayfieldBounds.PlayfieldRect;
        var height = playfield.height;
        var paddleHeight = height * _lockedY;
        return playfield.yMin + paddleHeight;
    }
    void RefreshBoundsSources()
    {
        _childColliders = GetComponentsInChildren<Collider>();
    }

    bool TryGetMouseWorldPosition(out Vector3 worldPosition)
    {
        var pos = _targetCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        worldPosition = pos;
        return true;
    }

    float ClampPaddlePivotX(float desiredPivotX)
    {
        Rect playfield = DynamicPlayfieldBounds.PlayfieldRect;

        if (playfield.width <= 0f)
        {
            Debug.LogWarning("Playfield width is 0. Paddle will not be constrained.");
            return desiredPivotX;
        }

        if (!TryGetPaddleWorldBounds(out Bounds paddleBounds))
        {
            Debug.LogWarning("No paddle bounds found.");
            return Mathf.Clamp(desiredPivotX, playfield.xMin, playfield.xMax);
        }

        // These offsets account for the parent pivot not being centered.
        float leftEdgeOffsetFromPivot = paddleBounds.min.x - transform.position.x;
        float rightEdgeOffsetFromPivot = paddleBounds.max.x - transform.position.x;

        float minAllowedPivotX = playfield.xMin + _edgePadding - leftEdgeOffsetFromPivot;
        float maxAllowedPivotX = playfield.xMax - _edgePadding - rightEdgeOffsetFromPivot;

        // If the paddle is wider than the available playfield, center it as best as possible.
        if (minAllowedPivotX > maxAllowedPivotX)
        {
            Debug.LogWarning("Paddle is wider than available playfield. Centering as best as possible.");
            float paddleCenterOffsetFromPivot =
                (leftEdgeOffsetFromPivot + rightEdgeOffsetFromPivot) * 0.5f;

            return playfield.center.x - paddleCenterOffsetFromPivot;
        }

        return Mathf.Clamp(desiredPivotX, minAllowedPivotX, maxAllowedPivotX);
    }

    bool TryGetPaddleWorldBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (Collider childCollider in _childColliders)
        {
            if (childCollider == null || !childCollider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = childCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(childCollider.bounds);
            }
        }
        return hasBounds;
    }
}
