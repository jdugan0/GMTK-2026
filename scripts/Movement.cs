using Godot;

public partial class Movement : CharacterBody2D
{
    [Export]
    public float MaxSpeed = 1200f;

    [Export]
    public float Acceleration = 7500f;

    [Export]
    public float Friction = 8000f;

    public bool moveEnabled = true;

    [Export]
    private Camera2D camera;

    [Export]
    private float mouseCameraWeight;

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

        if (moveEnabled)
        {
            MoveAndSlide();
        }
    }
}
