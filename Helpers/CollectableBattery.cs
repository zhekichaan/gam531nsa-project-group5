using FinalProject.Common;
using OpenTK.Mathematics;

namespace FinalProject.Helpers;

public class CollectibleBattery : WorldObject
{
    public bool IsCollected { get; private set; }
    public float BatteryRechargeAmount { get; private set; }
    private float _rotationSpeed;
    private float _bobSpeed;
    private float _bobHeight;
    private float _elapsedTime;
    private Vector3 _basePosition;

    public CollectibleBattery(Mesh mesh, Vector3 position, Vector3 scale, float rechargeAmount = 40f)
        : base(mesh, position, scale, 0f, false) // No collision
    {
        IsCollected = false;
        BatteryRechargeAmount = rechargeAmount;
        _rotationSpeed = 1.5f; // Rotation speed
        _bobSpeed = 2f; // Up/down bob speed
        _bobHeight = 0.15f; // How high it bobs up and down
        _elapsedTime = 0f;
        _basePosition = position;
    }

    public void Update(float deltaTime)
    {
        if (IsCollected) return;

        _elapsedTime += deltaTime;

        // Rotate continuously
        Rotation = _elapsedTime * _rotationSpeed;

        // Bob up and down
        float bobOffset = MathF.Sin(_elapsedTime * _bobSpeed) * _bobHeight;
        Position = _basePosition + new Vector3(0f, bobOffset, 0f);
    }

    public bool IsPlayerNearby(Vector3 playerPosition, float pickupRadius = 2.5f)
    {
        if (IsCollected) return false;
        float distance = Vector3.Distance(playerPosition, Position);
        return distance <= pickupRadius;
    }

    public void Collect()
    {
        IsCollected = true;
    }

    public void Reset()
    {
        IsCollected = false;
        _elapsedTime = 0f;
        Position = _basePosition;
        Rotation = 0f;
    }

    public override void UpdateTransform()
    {
        if (IsCollected) return;

        base.UpdateTransform();
    }
}