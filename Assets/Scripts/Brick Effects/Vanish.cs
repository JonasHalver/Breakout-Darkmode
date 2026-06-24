using UnityEngine;
using UnityEngine.ProBuilder;

public class Vanish : MonoBehaviour, IBrickBreakEffect
{
    Brick _brick;
    Mesh _mesh;
    IVFXSource _vfxSource;
    [SerializeField] AudioSource _audioSource; // Should be handled by the SFXHandler and an interface, but we're running out of time 
    [SerializeField] AudioClip _audioClip;
    
    public void Initialize(Brick brick, IVFXSource vfxSource)
    {
        _brick = brick;
        _vfxSource = vfxSource;
        if (brick.TryGetComponent(out MeshFilter mf))
        {
            _mesh = mf.mesh;
        }
        
        _vfxSource?.Initialize(_mesh, brick.transform);
    }
    
    public void Break()
    {
        _vfxSource.Detach();
        _audioSource.transform.SetParent(null);
        _audioSource.PlayOneShot(_audioClip, 1f);
        var velocity = Vector3.zero;
        if (_brick.TryGetComponent(out Rigidbody rb))
        {
            velocity = rb.linearVelocity;
        }
        _vfxSource.Play(300, velocity);
        gameObject.SetActive(false);
    }
    
    public void ResetBrick()
    {
        _vfxSource.Reattach();
        _audioSource.transform.SetParent(transform);
        gameObject.SetActive(true);
    }
}
