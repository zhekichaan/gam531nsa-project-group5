using FinalProject.Common;
using OpenTK.Mathematics;

namespace FinalProject.Common;

public class WorldObject
{
    private readonly Mesh _mesh;
    private readonly Vector3 _position;
    private readonly Vector3 _scale;
    private readonly float _rotation;
    private readonly bool _hasCollision;

    public BoundingBox CollisionBox { get; private set; }
    
    private Vector3? _customCollisionSize;

    public WorldObject(Mesh mesh, Vector3 position, Vector3 scale, float rotation,
        bool hasCollision = true, Vector3? customCollisionSize = null)
    {
        _mesh = mesh;
        _position = position;
        _scale = scale;
        _rotation = rotation;
        _hasCollision = hasCollision;
        _customCollisionSize = customCollisionSize;

        UpdateBoundingBox();
    }

    public virtual void UpdateTransform()
    {
        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateScale(_scale);
        model *= Matrix4.CreateRotationY(_rotation);
        model *= Matrix4.CreateTranslation(_position);
        _mesh.Transform = model;
    }

    public void UpdateBoundingBox()
    {
        if (_customCollisionSize.HasValue)
        {
            // Use custom collision box size
            CollisionBox = BoundingBox.FromCenterAndSize(_position, _customCollisionSize.Value, Vector3.One);
        }
        else
        {
            CollisionBox = BoundingBox.Transform(
                _mesh.OriginalMin,
                _mesh.OriginalMax,
                _position,
                _scale,
                _rotation
            );
        }
    }

    public void Draw()
    {
        UpdateTransform();

        _mesh.Draw();
    }

    public bool CheckCollision(BoundingBox other)
    {
        if (!_hasCollision) return false;
        return CollisionBox.Intersects(other);
    }
}