using System.Threading.Tasks;
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

    [Export]
    private float safetyTime;

    [Export]
    private float enemyKnockback;

    [Export]
    private float stunTime;

    // timers
    private double ripTimer;
    private double safetyTimer;

    [ExportGroup("Scenes")]
    [Export]
    private PackedScene bulletScene;

    [ExportGroup("Timer")]
    [Export]
    public double countDown;
    private double lostRip = 0;
    private float stunTimer = 0;
    private bool playedRip = false;

    public override void _Ready()
    {
        ripTimer = ripTime;
    }

    public void Attack(float damage, Node2D attacker)
    {
        if (safetyTimer <= 0)
        {
            safetyTimer = safetyTime;
            countDown -= damage;
            Vector2 dir = attacker.GlobalPosition - GlobalPosition;
            Velocity -= dir.Normalized() * enemyKnockback;
            moveEnabled = false;
            stunTimer = stunTime;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        safetyTimer -= dt;
        stunTimer -= dt;
        if (stunTimer <= 0)
        {
            moveEnabled = true;
        }
        Vector2 input = Input.GetVector("LEFT", "RIGHT", "UP", "DOWN");
        if (!moveEnabled)
        {
            input = Vector2.Zero;
        }
        Vector2 targetVelocity = input * MaxSpeed;

        camera.GlobalPosition =
            (GlobalPosition + mouseCameraWeight * GetGlobalMousePosition())
            / (1 + mouseCameraWeight);

        float rate = input == Vector2.Zero ? Friction : Acceleration;
        Velocity = Velocity.MoveToward(targetVelocity, rate * dt);
        if (Velocity.LengthSquared() > 0.05)
        {
            countDown -= delta;
        }
        if (countDown <= 0)
        {
            _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("level_test");
            GameManager.instance.Die();
            return;
        }
        MoveAndSlide();

        // attacking
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        if (Input.IsActionJustPressed("ATTACK"))
        {
            lostRip = 0;
            playedRip = false;
            var x = AudioManager.instance.PlaySFX("ripStart");
            x.p.Finished += () =>
            {
                if (!playedRip && Input.IsActionPressed("ATTACK"))
                {
                    AudioManager.instance.PlaySFX("ripLoop");
                }
            };
        }
        if (Input.IsActionPressed("ATTACK"))
        {
            if (ripTimer > 0)
            {
                // GD.Print(ripTimer);
                ripTimer -= delta;
                countDown -= delta * (attackCountdownCost / ripTime);
                lostRip += delta * (attackCountdownCost / ripTime);
                playedRip = false;
            }
            else
            {
                if (!playedRip)
                {
                    AudioManager.instance.CancelSFX("ripStart");
                    AudioManager.instance.CancelSFX("ripLoop");
                    AudioManager.instance.PlaySFX("ripEnd");
                    playedRip = true;
                }
            }
        }
        if (Input.IsActionJustReleased("ATTACK"))
        {
            if (ripTimer <= 0)
            {
                Bullet b = bulletScene.Instantiate<Bullet>();
                b.Construct(attackDamage, attackRange, attackSpeed, mouseDir, GlobalPosition);
                GetTree().Root.AddChild(b);
                AudioManager.instance.PlaySFX("throw");
            }
            else
            {
                countDown += lostRip;
                AudioManager.instance.CancelSFX("ripStart");
                AudioManager.instance.CancelSFX("ripLoop");
                playedRip = true;
            }
            ripTimer = ripTime;
        }
    }
}
