using UnityEngine;

public class BrickFactory : MonoBehaviour
{
    public static Brick Create(BrickSpawnRequest request, Transform parent)
    {
        if (request.Config == null)
        {
            Debug.LogError("BrickFactory received a request with no config.");
            return null;
        }

        GameObject newBrick;

        if (request.Config.Prefab == null)
        {
            newBrick = CreateGeneratedBrick();
        }
        else
        {
            newBrick = Instantiate(request.Config.Prefab);
        }

        if (!newBrick.TryGetComponent(out Brick brick))
        {
            Debug.LogError(
                $"Brick prefab '{request.Config.name}' does not have a Brick component on its root.",
                newBrick
            );

            Destroy(newBrick);
            return null;
        }

        newBrick.name = $"{newBrick.name} - {request.CollisionNote}";

        var brickTransform = newBrick.transform;
        var newRotation = Quaternion.Euler(
            new Vector3(brickTransform.rotation.eulerAngles.x, 
                brickTransform.rotation.eulerAngles.y, 
                request.Rotation.eulerAngles.z));
        brickTransform.SetParent(parent, true);
        brickTransform.SetPositionAndRotation(
            request.Position,
            newRotation
        );

        if (newBrick.TryGetComponent(out Renderer renderer))
        {
            renderer.enabled = false;
        }

        brick.Initialize(request.Config, request.CollisionNote);
        brick.SetSize(request.Size);

        return brick;
    }

    static GameObject CreateGeneratedBrick()
    {
        Debug.LogWarning("Spawned a brick with no prefab.");
        var newBrick = GameObject.CreatePrimitive(PrimitiveType.Cube);

        newBrick.AddComponent<Brick>();
        newBrick.AddComponent<EmissiveMaterialHandler>();
        newBrick.AddComponent<Vanish>();

        var rigidbody = newBrick.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX 
            | RigidbodyConstraints.FreezePositionZ;

        return newBrick;
    }
}
public struct BrickSpawnRequest
{
    public BrickConfigSO Config;
    public Vector3 Position;
    public Vector3 Size;
    public Quaternion Rotation;
    public string CollisionNote;
}