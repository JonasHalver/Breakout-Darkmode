using System.Collections.Generic;
using UnityEngine;

public class BreakApart : MonoBehaviour, IBrickBreakEffect
{
    List<(Transform, Vector3, Quaternion)> _childBasePositions = new();
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _audioClip;
    
    public void Initialize(Brick brick, IVFXSource vfxSource)
    {
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out EmissiveMaterialHandler blink))
                {
                    blink.Initialize((_ => {}));
                }
                _childBasePositions.Add((child, child.localPosition, child.localRotation));
            }
        }
    }

    public void Break()
    {
        _audioSource.PlayOneShot(_audioClip, 1f);
        _audioSource.transform.SetParent(null);
        if (_childBasePositions.Count > 0)
        {
            foreach ((Transform child, Vector3 basePosition, Quaternion baseRotation) tvq in _childBasePositions)
            {
                tvq.child.gameObject.SetActive(true);
                if(tvq.child.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddExplosionForce(10, transform.position, 1.0f, 0f, ForceMode.Impulse);
                }
            }
            transform.DetachChildren();
        }
        gameObject.SetActive(false);
    }

    public void ResetBrick()
    {
        _audioSource.transform.SetParent(transform);
        if (_childBasePositions.Count > 0)
        {
            foreach ((Transform child, Vector3 basePosition, Quaternion baseRotation) tvq in _childBasePositions)
            {
                tvq.child.SetParent(transform);
                tvq.child.localPosition = tvq.basePosition;
                tvq.child.localRotation = tvq.baseRotation;
                if (tvq.child.TryGetComponent(out Rigidbody rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}
