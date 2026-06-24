using UnityEngine;

public class Explode : MonoBehaviour, IBrickBreakEffect
{
    [SerializeField] float radius = 1f;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _audioClip;
    Brick _brick;
    IVFXSource _vfxSource;
    Mesh _mesh;

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
        var velocity = Vector3.zero;
        _audioSource.transform.SetParent(null);
        _audioSource.PlayOneShot(_audioClip, 1f);
        _vfxSource.Play(300, velocity);
        Collider[] others = new Collider[100];
        var othersCount = Physics.OverlapSphereNonAlloc(transform.position, radius, others);
        for (int i = 0; i < othersCount; i++)
        {
            var other = others[i];
            if (!other.TryGetComponent(out Rigidbody rb))
            {
                continue;
            }
            rb.AddExplosionForce(100f, transform.position, radius, 0f, ForceMode.Impulse);
        }
        gameObject.SetActive(false);
    }
    public void ResetBrick()
    {
        _vfxSource.Reattach();
        _audioSource.transform.SetParent(transform);
        gameObject.SetActive(true);
    }   
}
