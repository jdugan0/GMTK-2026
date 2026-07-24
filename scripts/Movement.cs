using System.Threading.Tasks;
using Godot;

public partial class Movement : CharacterBody2D
{
    [ExportGroup("Movement")]
    [Export]
    public float WalkSpeed = 475f;

    [Export]
    public float SprintSpeed = 800f;

    [Export]
    public float BulletSpeed = 300f;

    [Export]
    public float MoveCostFactor = 5e-6f;

    [Export]
    public float Acceleration = 7500f;

    [Export]
    public float Friction = 8000f;

    [Export]
    float footstepSoundDelayWalk = 0.1f;

    [Export]
    float footstepSoundDelaySprint = 0.05f;

    public bool moveEnabled = true;

    [ExportGroup("Camera")]
    [Export]
    float zoomed;

    [Export]
    private Camera2D camera;

    [Export]
    private float mouseCameraWeight;

    [Export]
    private float shakeStrength = 30f;

    [Export]
    private float shakeDecay = 4f;

    [Export]
    private float shakePerHit = 0.7f;

    private float shakeTrauma = 0f;

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
    private double footstepTimer;

    [ExportGroup("Scenes")]
    [Export]
    private PackedScene bulletScene;

    [Export]
    private AnimatedSprite2D sprite2D;

    [Export]
    private UI ui;

    [Export]
    Node2D flashlight;

    [Export]
    Sprite2D arrow;

    Node2D exit;

    [ExportGroup("Cursor")]
    [Export]
    private Texture2D cursorNormal;

    [Export]
    private Texture2D cursorMeat;

    [Export]
    private Vector2 cursorNormalHotspot = new Vector2(5, 0);

    [Export]
    private Vector2 cursorMeatHotspot = new Vector2(68, 42);

    [Export]
    private float cursorScale = 0.5f;

    private bool cursorIsMeat;

    [ExportGroup("Timer")]
    [Export]
    public double countDown;
    private float stunTimer = 0;
    private bool playedRip = false;

    float cameraZoomDefault;

    public float initalCountdown;

    public override void _Ready()
    {
        ripTimer = ripTime;
        exit = (Node2D)GetTree().GetFirstNodeInGroup("exit");
        cameraZoomDefault = camera.Zoom.X;
        initalCountdown = 50;
        cursorNormal = ScaleCursorTexture(cursorNormal);
        cursorMeat = ScaleCursorTexture(cursorMeat);
        cursorNormalHotspot *= cursorScale;
        cursorMeatHotspot *= cursorScale;
        cursorIsMeat = true;
        SetCursor(false);
    }

    private Texture2D ScaleCursorTexture(Texture2D texture)
    {
        Image image = texture.GetImage();
        if (image.IsCompressed())
        {
            image.Decompress();
        }
        image.Resize(
            Mathf.Max(1, Mathf.RoundToInt(image.GetWidth() * cursorScale)),
            Mathf.Max(1, Mathf.RoundToInt(image.GetHeight() * cursorScale))
        );
        return ImageTexture.CreateFromImage(image);
    }

    private void SetCursor(bool meat)
    {
        if (meat == cursorIsMeat)
        {
            return;
        }
        cursorIsMeat = meat;
        Input.SetCustomMouseCursor(
            meat ? cursorMeat : cursorNormal,
            Input.CursorShape.Arrow,
            meat ? cursorMeatHotspot : cursorNormalHotspot
        );
    }

    public void Hit(float damage, Node2D attacker)
    {
        if (safetyTimer <= 0)
        {
            AudioManager.instance.PlaySFX("playerHurt");
            safetyTimer = safetyTime;
            countDown -= damage;
            Vector2 dir = attacker.GlobalPosition - GlobalPosition;
            Velocity -= dir.Normalized() * enemyKnockback;
            moveEnabled = false;
            stunTimer = stunTime;
            shakeTrauma = Mathf.Min(shakeTrauma + shakePerHit, 1f);
            ui.Loss((int)damage);
        }
    }

    private void UpdateAnimation(Vector2 lookDir)
    {
        // 0 = right, 90 = down, -90 = up, +/-180 = left
        float angle = Mathf.RadToDeg(lookDir.Angle());

        if (Input.IsActionPressed("ATTACK") && ripTimer > 0)
        {
            int ripFrame = ripTimer > ripTime / 2 ? 0 : 1;
            if (angle > 45f && angle < 135f) // down
                PlayRip("DOWN_RIP", flip: false, frame: ripFrame);
            else if (angle >= -90f && angle <= 45f) // up through down-right
                PlayRip("RIGHT_RIP", flip: false, frame: ripFrame);
            else // up through down-left
                PlayRip("RIGHT_RIP", flip: true, frame: ripFrame);
            return;
        }

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
            && footstepTimer <= 0
        )
        {
            footstepTimer = Input.IsActionPressed("SPRINT")
                ? footstepSoundDelaySprint
                : footstepSoundDelayWalk;
            if (countDown <= initalCountdown * (1f / 3f))
            {
                AudioManager.instance.PlaySFX("footstepsGoopMore");
            }
            else if (countDown <= initalCountdown * (2f / 3f))
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

    private void PlayRip(string anim, bool flip, int frame)
    {
        sprite2D.FlipH = flip;
        sprite2D.Animation = anim;
        sprite2D.Frame = frame;
        sprite2D.Pause();
    }

    public override void _PhysicsProcess(double delta)
    {
        footstepTimer -= delta;
        float dt = (float)delta;
        float angleToExit = (exit.GlobalPosition - GlobalPosition).Angle();
        arrow.GlobalPosition = GlobalPosition;
        arrow.Rotation = angleToExit;
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
        if (input == Vector2.Zero)
        {
            ui.Beat(0);
        }
        else if (!Input.IsActionPressed("SPRINT"))
        {
            ui.Beat(1);
        }
        else
        {
            ui.Beat(2);
        }
        Vector2 targetVelocity = input * maxSpeed;

        camera.GlobalPosition =
            (GlobalPosition + mouseCameraWeight * GetGlobalMousePosition())
            / (1 + mouseCameraWeight);
        if (shakeTrauma > 0f)
        {
            shakeTrauma = Mathf.Max(shakeTrauma - shakeDecay * dt, 0f);
            float amount = shakeTrauma * shakeTrauma;
            camera.Offset =
                new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0))
                * shakeStrength
                * amount;
        }
        else
        {
            camera.Offset = Vector2.Zero;
        }
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
            maxSpeed = BulletSpeed;
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
                playedRip = false;
                float deltaZoom = ((zoomed - cameraZoomDefault) / ripTime) * dt;
                camera.Zoom += new Vector2(deltaZoom, deltaZoom);
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
                camera.Zoom = new Vector2(cameraZoomDefault, cameraZoomDefault);
                countDown -= attackCountdownCost;
                ui.Loss((int)attackCountdownCost);
            }
            else
            {
                AudioManager.instance.CancelSFX("ripStart");
                AudioManager.instance.CancelSFX("ripLoop");
                AudioManager.instance.CancelSFX("ripEnd");
                playedRip = true;
            }
            ripTimer = ripTime;
        }

        SetCursor(ripTimer <= 0);
    }
}
