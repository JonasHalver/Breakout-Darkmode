using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Brick Config")]
public class BrickConfigSO : ConfigBase
{
    [SerializeField] GameObject _prefab;
    [SerializeField] string _csvTypeCode = "S";
    [SerializeField] float _maxHealth = 1000;
    [SerializeField] bool _isIndestructible;
    [SerializeField] PhysicsMaterial _physicsMaterial;
    public PhysicsMaterial PhysicsMaterial => _physicsMaterial;
    public GameObject Prefab => _prefab;
    public float MaxHealth => _maxHealth;
    public bool IsIndestructible => _isIndestructible;
    

    public char CsvTypeCode
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_csvTypeCode))
            {
                return '\0';
            }

            return char.ToUpperInvariant(_csvTypeCode.Trim()[0]);
        }
    }
}
