using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PowerUp")]
public class PowerUpConfigSO : ConfigBase
{
    
    [Header("Visuals")] 
    [SerializeField] Material _material;

    [Header("Movement")] 
    [SerializeField] float _pickupLifetime;
    [SerializeField] float _powerUpLifetime;
    [SerializeField] float _gravityMultiplier;

    [Header("Gameplay")] 
    [SerializeField] PowerUpType _type;
    
    public Material Material => _material;
    public float PickupLifetime => _pickupLifetime;
    public float PowerUpLifetime => _powerUpLifetime;
    
    public float GravityMultiplier => _gravityMultiplier;
    public PowerUpType Type => _type;
}
public enum PowerUpType
{
    IncreaseMass,
    SplitBall,
    FastBall,
    MultiBall
}
