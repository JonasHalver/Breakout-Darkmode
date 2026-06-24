using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmissiveMaterialHandler : MonoBehaviour
{
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    Renderer _renderer;
    [SerializeField] BlinkConfigSO _blinkConfig;
    Coroutine _blinkCoroutine;
    Color _baseColor;
    readonly List<Renderer> _renderers = new();
    bool _useChildrenExclusively;
    bool _rampingIntensity;
    bool _blinkOnCollision;
    float _intensitySetByRamp;
    Color _colorSetByRamp;
    public BlinkConfigSO Config
    {
        get => _blinkConfig;
        set => _blinkConfig = value;
    }

    public void Initialize(Action<bool> onInitialized, bool blinkOnCollision = true)
    {
        _blinkOnCollision = blinkOnCollision;
        if (!TryGetComponent(out _renderer))
        {
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent(out Renderer rend))
                {
                    continue;
                }

                _useChildrenExclusively = true;
                _renderers.Add(rend);
            }
            if (_renderers.Count == 0)
            {
                onInitialized?.Invoke(false);
                return;
            }
        }

        if (_useChildrenExclusively)
        {
            foreach (var r in _renderers)
            {
                r.material = _blinkConfig.BlinkMaterial;
            }

            SetBaseColors();
        }
        else
        {
            _renderer.material = _blinkConfig.BlinkMaterial;
            SetBaseColor();
        }
        onInitialized?.Invoke(true);
    }

    void SetBaseColor()
    {
        var mat = _renderer.material;
        mat.SetColor(BaseColor, Config.BaseColor);
        mat.SetColor(EmissionColor, Config.BaseColor);
        _baseColor = Config.BaseColor;
    }
    void SetBaseColors()
    {
        foreach (var r in _renderers)
        {
            var mat = r.material;
            mat.SetColor(BaseColor, Config.BaseColor);
            mat.SetColor(EmissionColor, Config.BaseColor);
            _baseColor = Config.BaseColor;
        }
    }
    void Blink()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            if (_useChildrenExclusively)
            {
                ResetColors();
            }
            else
            {
                ResetColor();
            }
        }
        if (gameObject.activeSelf)
        {
            _blinkCoroutine = StartCoroutine(BlinkSequence());
        }
    }
    IEnumerator BlinkSequence()
    {
        var timeElapsed = 0f;
        while (timeElapsed < Config.BlinkDuration)
        {
            var intensityT = Config.BlinkIntensityCurve.Evaluate(timeElapsed / Config.BlinkDuration);
            var gradC = Color.Lerp(_rampingIntensity ?_colorSetByRamp:_baseColor, Color.white, intensityT);
            
            var intensity = intensityT * Config.BlinkIntensityMax;
            intensity += 1;
            intensity = Mathf.Max(intensity, _intensitySetByRamp);
            if (_useChildrenExclusively)
            {
                foreach (var r in _renderers)
                {
                    r.material.SetColor(EmissionColor, gradC * intensity);
                    r.material.SetColor(BaseColor, gradC);
                }
            }
            else
            {
                _renderer.material.SetColor(EmissionColor, gradC * intensity);
                _renderer.material.SetColor(BaseColor, gradC);
            }
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if (_useChildrenExclusively)
        {
            ResetColors();
        }
        else
        {
            ResetColor();
        }
    }

    void ResetColor()
    {
        _renderer.material.SetColor(BaseColor, _baseColor);
        _renderer.material.SetColor(EmissionColor, _baseColor);
    }
    void ResetColors()
    {
        foreach (var r in _renderers)
        {
            r.material.SetColor(BaseColor, _baseColor);
            r.material.SetColor(EmissionColor, _baseColor);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (!_blinkOnCollision)
        {
            return;
        }
        Blink();
    }

    public void IntensityRamp(float time, bool setAsNewDefault = false)
    {
        _rampingIntensity = true;
        var gradC = Config.IntensityRampGradient.Evaluate(time);
        var intensity = Config.IntensityRampCurve.Evaluate(time) * Config.IntensityRampMax;
        _intensitySetByRamp = intensity;
        _colorSetByRamp = gradC;
        if (_useChildrenExclusively)
        {
            foreach (var r in _renderers)
            {
                r.material.SetColor(EmissionColor, gradC * intensity);
                r.material.SetColor(BaseColor, gradC);
            }
        }
        else
        {
            _renderer.material.SetColor(EmissionColor, gradC * intensity);
            _renderer.material.SetColor(BaseColor, gradC);
        }

        if (setAsNewDefault) // Move the color towards full white, so we can see its health
        {
            _baseColor = gradC;
            _rampingIntensity = false;
        }
    }

    public void EndIntensityRamp()
    {
        _rampingIntensity = false;
        if (_useChildrenExclusively)
        {
            ResetColors();
        }
        else
        {
            ResetColor();
        }
    }
}
