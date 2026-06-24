using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Level Config")]
public class BrickLayoutConfigSO : ConfigBase
{
    [Header("CSV")]
    [SerializeField] TextAsset _csvLayout;
    [Header("Brick Field Area")]
    [SerializeField, Range(0f, 1f)] float _leftPercent = 0.05f;
    [SerializeField, Range(0f, 1f)] float _rightPercent = 0.05f;
    [SerializeField, Range(0f, 1f)] float _topPercent = 0.08f;
    [SerializeField, Range(0f, 1f)] float _bottomPercent = 0.55f;

    [Header("Field Padding")]
    [SerializeField] Vector2 _fieldPadding = new Vector2(0.1f, 0.1f);

    [SerializeField] Vector2 _spacing = new Vector2(0.08f, 0.08f);

    public float LeftPercent => _leftPercent;
    public float RightPercent => _rightPercent;
    public float TopPercent => _topPercent;
    public float BottomPercent => _bottomPercent;
    public Vector2 FieldPadding => _fieldPadding;
    public Vector2 Spacing => _spacing;
    public TextAsset CsvLayout => _csvLayout;
}
