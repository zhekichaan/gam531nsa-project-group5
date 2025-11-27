using FinalProject.Common;
using OpenTK.Mathematics;

namespace FinalProject.Helpers;

public class FlashlightObject : WorldObject
{
    private float _pitch;
    private float _yaw;
    
    public FlashlightObject(Mesh mesh) 
        : base(mesh, Vector3.Zero, new Vector3(0.05f), 0f, false)
    {
    }
    
    public void UpdateFromCamera(Camera camera, Vector3 offset)
    {
        // Store camera angles
        _pitch = MathHelper.DegreesToRadians(camera.Pitch);
        _yaw = MathHelper.DegreesToRadians(camera.Yaw);
        
        // Calculate world position
        Vector3 worldOffset = 
            camera.Right * offset.X +
            camera.Up * offset.Y +
            camera.Front * offset.Z;
        
        Position = camera.Position + worldOffset;
    }
    
    public override void UpdateTransform()
    {
        Matrix4 model = Matrix4.Identity;
        
        // Scale
        model *= Matrix4.CreateScale(Scale);
        
        // Apply camera rotation (pitch then yaw)
        model *= Matrix4.CreateRotationX(_pitch);
        model *= Matrix4.CreateRotationY(-_yaw);
        
        model *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-90f));
        
        // Translate
        model *= Matrix4.CreateTranslation(Position);
        
        Mesh.Transform = model;
    }
}