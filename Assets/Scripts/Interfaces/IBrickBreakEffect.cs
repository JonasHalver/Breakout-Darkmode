using UnityEngine;

public interface IBrickBreakEffect
{
    public void Initialize(Brick brick, IVFXSource vfxSource);
    public void Break();
    public void ResetBrick();
}
