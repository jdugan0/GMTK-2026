using System.Threading.Tasks;
using Godot;

public partial class Movement : CharacterBody2D
{
    [ExportGroup("Movement")]
    [Export]
    public float WalkSpeed = 475f;

    [Export]
    public float SprintSpeed = 800f;

    // countdown lost per second = speed^2 * this, so faster movement is disproportionately expensive
    [Export]
    public float MoveCostFactor = 5e-6f;

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
    private float attackSpeed;

    [Export]
    private float throwKnockback;

    [Export]
    private float throwStun;

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

    [Export]
    private AnimatedSprite2D sprite2D;

    [Export]
    Node2D flashlight;

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

    private void UpdateAnimation(Vector2 lookDir)
    {
        // 0 = right, 90 = down, -90 = up, +/-180 = left
        float angle = Mathf.RadToDeg(lookDir.Angle());

        if (angle > 67.5f && angle < 112.5f) // down
            Play("FRONT", flip: false);
        else if (angle >= 22.5f && angle <= 67.5f) // down-right
            Play("FRONTD", flip: false);
        else if (angle >= 112.5f && angle <= 157.5f) // down-left
            Play("FRONTD", flip: true);
        else if (angle >= -22.5f && angle <= 22.5f) // right
            Play("SIDE", flip: false);
        else if (angle > 157.5f || angle < -157.5f) // left
            Play("SIDE", flip: true);
        else if (angle < -22.5f && angle >= -67.5f) // up-right
            Play("BACKD", flip: true);
        else if (angle <= -112.5f && angle > -157.5f) // up-left
            Play("BACKD", flip: false);
        else // up
            Play("BACK", flip: false);
    }

    private void PlayFootstep()
    {
        if (
            AudioManager.instance.GetPlaying("footsteps").Count == 0
            && AudioManager.instance.GetPlaying("footstepsGoop").Count == 0
            && AudioManager.instance.GetPlaying("footstepsGoopMore").Count == 0
        )
        {
            if (countDown <= 33)
            {
                AudioManager.instance.PlaySFX("footstepsGoopMore");
            }
            else if (countDown <= 66)
            {
                AudioManager.instance.PlaySFX("footstepsGoop");
            }
            else
            {
                AudioManager.instance.PlaySFX("footsteps");
            }
        }
    }

    private void Play(string anim, bool flip)
    {
        sprite2D.FlipH = flip;
        sprite2D.Play(anim);
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
        float maxSpeed = Input.IsActionPressed("SPRINT") ? SprintSpeed : WalkSpeed;
        Vector2 targetVelocity = input * maxSpeed;

        camera.GlobalPosition =
            (GlobalPosition + mouseCameraWeight * GetGlobalMousePosition())
            / (1 + mouseCameraWeight);
        if (input != Vector2.Zero)
        {
            PlayFootstep();
        }
        float rate = input == Vector2.Zero ? Friction : Acceleration;
        Velocity = Velocity.MoveToward(targetVelocity, rate * dt);
        float speed = Velocity.Length();
        if (speed > 0.05f)
        {
            countDown -= speed * speed * MoveCostFactor * delta;
        }
        if (countDown <= 0)
        {
            GameManager.instance.Die(this);
            return;
        }
        MoveAndSlide();

        // attacking
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        flashlight.Rotation = mouseDir.Angle();
        UpdateAnimation(mouseDir);
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
                b.Construct(throwKnockback, throwStun, attackSpeed, mouseDir, GlobalPosition);
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
