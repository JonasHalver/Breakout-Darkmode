using UnityEngine;
using UnityEngine.VFX;

public class BrickVFX : MonoBehaviour, IVFXSource
{
    [SerializeField] VisualEffect _visualEffect;
    static readonly int SourceMeshId = Shader.PropertyToID("SourceMesh");
    static readonly int BurstCountId = Shader.PropertyToID("BurstCount");
    static readonly int VelocityID = Shader.PropertyToID("InitialVelocity");
    Transform _parent;
    
    public void Initialize(Mesh mesh, Transform parent)
    {
        _parent = parent;
        _visualEffect.SetMesh(SourceMeshId, mesh);
    }

    public void Detach()
    {
        _visualEffect.transform.SetParent(null, true);
        _visualEffect.transform.localScale = Vector3.one;
    }

    public void Play(int burstCount, Vector3 velocity)
    {
        _visualEffect.SetVector3(VelocityID, velocity);
        _visualEffect.SetInt(BurstCountId, burstCount);
        _visualEffect.SendEvent("PlayEffect");
    }

    public void Reattach()
    {
        _visualEffect.transform.SetParent(_parent, true);
    }
}