using UnityEngine;

[CreateAssetMenu(fileName = "BlinkConfig", menuName = "Configs/Blink")]
public class BlinkConfigSO : ConfigBase
{
    [SerializeField] AnimationCurve _blinkIntensityCurve;
    [SerializeField] float _blinkIntensityMax;
    [SerializeField] float _blinkDuration;
    [SerializeField] Material _blinkMaterial;
    [SerializeField] Color _baseColor = Color.white;
    [SerializeField] AnimationCurve _intensityRampCurve;
    [SerializeField] float _intensityRampMax;
    [SerializeField] Gradient _intensityRampGradient;
    public AnimationCurve BlinkIntensityCurve => _blinkIntensityCurve;
    public float BlinkIntensityMax => _blinkIntensityMax;
    public float BlinkDuration => _blinkDuration;
    public Material BlinkMaterial => _blinkMaterial;
    public Color BaseColor => _baseColor;
    public AnimationCurve IntensityRampCurve => _intensityRampCurve;
    public float IntensityRampMax => _intensityRampMax;
    public Gradient IntensityRampGradient => _intensityRampGradient;
}
