using TMPro;
using UnityEngine;

public class HintHandler : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _spaceHint;
    [SerializeField] TextMeshProUGUI _escHint;

    [SerializeField] Color _colorMax;
    [SerializeField] Color _colorMin;

    float _timeSinceNotStuck;

    float _timeSinceStart;
 
    void Update()
    {
        if (Ball.BallIsStuck)
        {
            _timeSinceNotStuck += Time.deltaTime * 0.5f;
            _timeSinceNotStuck = Mathf.Clamp01(_timeSinceNotStuck);
            _spaceHint.color = Color.Lerp(_colorMin, _colorMax, _timeSinceNotStuck);
        }
        else
        {
            _timeSinceNotStuck = 0;
            _spaceHint.color = _colorMin;
        }
        _timeSinceStart += Time.deltaTime;
        _escHint.color = _timeSinceStart switch
        {
            > 60 and < 80 => _colorMax,
            > 80 => _colorMin,
            _ => _escHint.color
        };
    }
}
