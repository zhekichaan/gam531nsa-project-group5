using FinalProject.Common;
using OpenTK.Mathematics;

namespace FinalProject.Common;

public class WorldObject
{
    public readonly Mesh Mesh;
    public Vector3 Position;
    public readonly Vector3 Scale;
    public float Rotation;
    private readonly bool _hasCollision;

    public BoundingBox CollisionBox { get; private set; }

    private Vector3? _customCollisionSize;

    public WorldObject(Mesh mesh, Vector3 position, Vector3 scale, float rotation,
        bool hasCollision = true, Vector3? customCollisionSize = null)
    {
        Mesh = mesh;
        Position = position;
        Scale = scale;
        Rotation = rotation;
        _hasCollision = hasCollision;
        _customCollisionSize = customCollisionSize;

        UpdateBoundingBox();
    }

    public virtual void UpdateTransform()
    {
        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateScale(Scale);
        model *= Matrix4.CreateRotationY(Rotation);
        model *= Matrix4.CreateTranslation(Position);
        Mesh.Transform = model;
    }

    public void UpdateBoundingBox()
    {
        if (_customCollisionSize.HasValue)
        {
            // Use custom collision box size
            CollisionBox = BoundingBox.FromCenterAndSize(Position, _customCollisionSize.Value, Vector3.One);
        }
        else
        {
            CollisionBox = BoundingBox.Transform(
                Mesh.OriginalMin,
                Mesh.OriginalMax,
                Position,
                Scale,
                Rotation
            );
        }
    }

    public void Draw()
    {
        UpdateTransform();

        Mesh.Draw();
    }

    public bool CheckCollision(BoundingBox other)
    {
        if (!_hasCollision) return false;
        return CollisionBox.Intersects(other);
    }
}