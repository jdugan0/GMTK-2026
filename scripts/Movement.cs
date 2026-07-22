using Godot;

public partial class Movement : CharacterBody2D
{
    [ExportGroup("Movement")]
    [Export]
    public float MaxSpeed = 1200f;

    [Export]
    public float Acceleration = 7500f;

    [Export]
    public float Friction = 8000f;

    public bool moveEnabled = true;

    [ExportGroup("Camera")]
    [Export]
    private Camera2D camera;

    [Export]
    private float mouseCameraWeight;

    [ExportGroup("Combat")]
    [Export]
    private float ripTime;

    [Export]
    private float attackRange;

    [Export]
    private float attackDamage;

    [Export]
    private float attackSpeed;

    [Export]
    private float attackCountdownCost;

    // timers
    private double ripTimer;

    [ExportGroup("Scenes")]
    [Export]
    private PackedScene bulletScene;

    public override void _Ready()
    {
        ripTimer = ripTime;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        Vector2 input = Input.GetVector("LEFT", "RIGHT", "UP", "DOWN");
        Vector2 targetVelocity = input * MaxSpeed;

        camera.GlobalPosition =
            (GlobalPosition + mouseCameraWeight * GetGlobalMousePosition())
            / (1 + mouseCameraWeight);

        float rate = input == Vector2.Zero ? Friction : Acceleration;
        Velocity = Velocity.MoveToward(targetVelocity, rate * dt);

        // attacking
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        if (Input.IsActionPressed("ATTACK"))
        {
            if (ripTimer > 0)
            {
                // GD.Print(ripTimer);
                ripTimer -= delta;
                GameManager.instance.ApplyCountdownCost(attackCountdownCost);
            }
            else { }
        }
        if (Input.IsActionJustReleased("ATTACK"))
        {
            if (ripTimer <= 0)
            {
                Bullet b = bulletScene.Instantiate<Bullet>();
                b.Construct(
                    attackDamage,
                    attackRange,
                    attackSpeed,
                    mouseDir,
                    GlobalPosition + new Vector2(50, 50)
                );
                GetTree().Root.AddChild(b);
            }
            ripTimer = ripTime;
        }

        if (moveEnabled)
        {
            MoveAndSlide();
        }
    }
}
