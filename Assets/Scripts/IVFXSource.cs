using UnityEngine;

public interface IVFXSource
{
    public void Initialize(Mesh mesh, Transform parent);
    public void Detach();
    public void Play(int burstCount, Vector3 velocity);
    public void Reattach();
}