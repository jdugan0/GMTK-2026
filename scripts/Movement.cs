using System;
using System.Runtime.InteropServices;
using System.Threading;
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
    float zoomedMeat;

    [Export]
    float zoomHealth;

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
    public AnimatedSprite2D sprite2D;

    [Export]
    private UI ui;

    [Export]
    Flashlight flashlight;

    [Export]
    public Sprite2D arrow;

    [ExportGroup("Arrow")]
    [Export]
    private float arrowHideDelay = 4f;

    [Export]
    private float arrowFlickerDuration = 1.2f;

    [Export]
    private float arrowFlickerIntervalMin = 0.03f;

    [Export]
    private float arrowFlickerIntervalMax = 0.09f;

    private double arrowHideTimer;
    private double arrowFlickerTimer;
    private double arrowFlickerStepTimer;
    private bool arrowFlickerOff;

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

    [ExportGroup("Flash")]
    [Export]
    private float flashScale = 1.6f;

    [Export]
    private float flashDuration = 0.4f;

    [Export]
    private Color flashTint;

    private bool cursorIsMeat;

    [ExportGroup("Timer")]
    [Export]
    public double countDown;
    private float stunTimer = 0;
    private bool playedRip = false;
    private int walkFrame;

    float cameraZoomDefault;

    public float initalCountdown;

    [ExportGroup("Art")]
    [Export]
    private Texture2D[] damageSheets;

    [Export]
    GpuParticles2D bloodParticles;

    [Export]
    GpuParticles2D footParticles;

    [Export]
    int footEmitterCount = 3;

    [Export]
    RadialSlider radialSlider;

    [Export]
    Texture2D throwSheet;

    [Export]
    float throwTime;

    private GpuParticles2D[] footEmitters;
    private int footEmitterIndex;

    int prevId = -1;

    [Export]
    float lightEnergyFinal;

    private void SwapSheet(int id)
    {
        if (id == prevId)
            return;
        if (id < 0 || id >= damageSheets.Length)
            return;
        Texture2D sheet = damageSheets[id];
        var frames = sprite2D.SpriteFrames;
        foreach (string anim in frames.GetAnimationNames())
        {
            if (anim == "DEATH")
            {
                continue;
            }
            if (anim == "THROW")
            {
                for (int i = 0; i < frames.GetFrameCount(anim); i++)
                {
                    if (frames.GetFrameTexture(anim, i) is AtlasTexture throwAt)
                    {
                        throwAt.Atlas = throwSheet;
                        Rect2 region = throwAt.Region;
                        throwAt.Region = new Rect2(
                            region.Position.X,
                            id * region.Size.Y,
                            region.Size
                        );
                    }
                }
                continue;
            }
            for (int i = 0; i < frames.GetFrameCount(anim); i++)
            {
                if (frames.GetFrameTexture(anim, i) is AtlasTexture at)
                    at.Atlas = sheet;
            }
        }
        prevId = id;
    }

    float initalEnergy = 0;

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
        cameraZoomInital = camera.Zoom.X;
        BuildFootEmitters();
        initalEnergy = flashlight.Energy;
        if (LevelManager.instance.currLevel == 4)
        {
            arrowHideTimer = arrowHideDelay;
        }
    }

    private void UpdateArrowFlicker(double delta)
    {
        if (arrowHideTimer > 0)
        {
            arrowHideTimer -= delta;
            if (arrowHideTimer <= 0)
            {
                arrowFlickerTimer = arrowFlickerDuration;
                arrowFlickerStepTimer = 0;
                arrowFlickerOff = false;
            }
            return;
        }

        if (arrowFlickerTimer <= 0)
            return;

        arrowFlickerTimer -= delta;
        arrowFlickerStepTimer -= delta;

        if (arrowFlickerTimer <= 0)
        {
            arrow.Visible = false;
            return;
        }

        if (arrowFlickerStepTimer <= 0)
        {
            arrowFlickerOff = !arrowFlickerOff;
            float step = (float)GD.RandRange(arrowFlickerIntervalMin, arrowFlickerIntervalMax);
            float progress = 1f - (float)(arrowFlickerTimer / arrowFlickerDuration);
            arrowFlickerStepTimer = arrowFlickerOff ? step * (1f + progress * 4f) : step;
            arrow.Visible = !arrowFlickerOff;
        }
    }

    private void BuildFootEmitters()
    {
        footEmitters = new GpuParticles2D[Mathf.Max(1, footEmitterCount)];
        footEmitters[0] = footParticles;
        Node parent = footParticles.GetParent();
        for (int i = 1; i < footEmitters.Length; i++)
        {
            var copy = (GpuParticles2D)footParticles.Duplicate();
            parent.AddChild(copy);
            footEmitters[i] = copy;
        }
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
        if (meat)
        {
            SpawnReadyFlash();
        }
        Input.SetCustomMouseCursor(
            meat ? cursorMeat : cursorNormal,
            Input.CursorShape.Arrow,
            meat ? cursorMeatHotspot : cursorNormalHotspot
        );
    }

    private void SpawnReadyFlash()
    {
        var ghost = new Sprite2D
        {
            Texture = sprite2D.SpriteFrames.GetFrameTexture(sprite2D.Animation, sprite2D.Frame),
            FlipH = sprite2D.FlipH,
            Centered = sprite2D.Centered,
            Offset = sprite2D.Offset,
            Modulate = flashTint,
        };
        GetTree().CurrentScene.AddChild(ghost);
        ghost.GlobalTransform = sprite2D.GlobalTransform;

        var tween = ghost.CreateTween().SetParallel();
        tween
            .TweenProperty(ghost, "scale", ghost.Scale * flashScale, flashDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ghost, "modulate:a", 0f, flashDuration);
        tween.Chain().TweenCallback(Callable.From(ghost.QueueFree));
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
            flashlight.Flicker();
            ui.Loss((int)damage);
        }
    }

    float throwTimer = 0;

    private void UpdateAnimation(Vector2 lookDir)
    {
        // 0 = right, 90 = down, -90 = up, +/-180 = left
        float angle = Mathf.RadToDeg(lookDir.Angle());

        if (throwTimer > 0)
        {
            if (lookDir.X < 0)
            {
                sprite2D.FlipH = true;
            }
            else
            {
                sprite2D.FlipH = false;
            }
            sprite2D.Play("THROW");
            return;
        }

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
            // AudioManager.instance.GetPlaying("footsteps").Count == 0
            // && AudioManager.instance.GetPlaying("footstepsGoop").Count == 0
            // && AudioManager.instance.GetPlaying("footstepsGoopMore").Count == 0
            // &&
            footstepTimer <= 0
        )
        {
            footstepTimer = Input.IsActionPressed("SPRINT")
                ? footstepSoundDelaySprint
                : footstepSoundDelayWalk;
            walkFrame ^= 1;
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
            footEmitters[footEmitterIndex].Restart();
            footEmitterIndex = (footEmitterIndex + 1) % footEmitters.Length;
        }
    }

    private void Play(string anim, bool flip)
    {
        sprite2D.FlipH = flip;
        bool walking = Velocity.LengthSquared() > 25f;
        if (walking)
        {
            anim += "_WALK";
        }
        if (cursorIsMeat)
        {
            anim += "_MEAT";
        }
        if (walking)
        {
            sprite2D.Animation = anim;
            sprite2D.Frame = walkFrame % sprite2D.SpriteFrames.GetFrameCount(anim);
            sprite2D.Pause();
            return;
        }
        sprite2D.Play(anim);
    }

    private void PlayRip(string anim, bool flip, int frame)
    {
        sprite2D.FlipH = flip;
        sprite2D.Animation = anim;
        sprite2D.Frame = frame;
        sprite2D.Pause();
    }

    private void UpdateSprite()
    {
        SwapSheet(Mathf.RoundToInt((initalCountdown - countDown) / initalCountdown * 5));
    }

    float cameraZoomInital;

    public void Reset()
    {
        SwapSheet(0);
    }

    public void Gain(int a)
    {
        ui.Gain(a);
    }

    public override void _Process(double delta)
    {
        UpdateArrowFlicker(delta);
        radialSlider.Scale = Vector2.One / camera.Zoom;
        radialSlider.Position = -radialSlider.Size * radialSlider.Scale * 0.5f;
    }

    [Export]
    float drainTime;

    float drainTimer = 0;
    float maxSpeedScale = 1;

    public override void _PhysicsProcess(double delta)
    {
        UpdateSprite();
        footstepTimer -= delta;
        throwTimer -= (float)delta;
        LevelManager.instance.currLevel = 5;

        if (LevelManager.instance.currLevel == 5 && drainTimer <= drainTime)
        {
            drainTimer += (float)delta;
            maxSpeedScale = (1 - 0.6f * (drainTimer / drainTime));
        }

        double percentDrained = countDown / initalCountdown;
        flashlight.Energy = (float)(
            percentDrained * initalEnergy + (1 - percentDrained) * lightEnergyFinal
        );

        float dt = (float)delta;
        float angleToExit = (exit.GlobalPosition - GlobalPosition).Angle();
        if (ripTimer > 0)
        {
            radialSlider.Value = 1 - (ripTimer / ripTime);
        }
        else
        {
            radialSlider.Value = 0;
        }
        if (!Input.IsActionPressed("ATTACK"))
        {
            double z = countDown / initalCountdown;
            float x = (float)(z * cameraZoomInital + (1 - z) * zoomHealth);
            cameraZoomDefault = x;
            camera.Zoom = new Vector2(cameraZoomDefault, cameraZoomDefault);
        }
        if (Input.IsActionJustPressed("RESET"))
        {
            GameManager.instance.Die(this);
            return;
        }
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
        maxSpeed *= maxSpeedScale;
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
            countDown -= Math.Pow(maxSpeed / maxSpeedScale, 2) * MoveCostFactor * delta;
        }
        float rate = input == Vector2.Zero ? Friction : Acceleration;
        Velocity = Velocity.MoveToward(targetVelocity, rate * dt);
        float speed = Velocity.Length();
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
                float deltaZoom = ((zoomedMeat - cameraZoomDefault) / ripTime) * dt;
                camera.Zoom += new Vector2(deltaZoom, deltaZoom);
                bloodParticles.Emitting = true;
                bloodParticles.Rotation = mouseDir.Angle();
                float a = Mathf.RadToDeg(mouseDir.Angle());
                if (a > 45f && a < 135f)
                {
                    bloodParticles.ZIndex = 1;
                }
                else
                {
                    bloodParticles.ZIndex = 0;
                }
            }
            else
            {
                bloodParticles.Emitting = false;
                if (!playedRip)
                {
                    AudioManager.instance.CancelSFX("ripStart");
                    AudioManager.instance.CancelSFX("ripLoop");
                    AudioManager.instance.PlaySFX("ripEnd");
                    AudioManager.instance.PlaySFX("throwReady");
                    ui.Loss((int)attackCountdownCost);
                    playedRip = true;
                }
            }
        }
        if (Input.IsActionJustReleased("ATTACK"))
        {
            bloodParticles.Emitting = false;
            if (ripTimer <= 0)
            {
                Bullet b = bulletScene.Instantiate<Bullet>();
                b.Construct(throwKnockback, throwStun, attackSpeed, mouseDir, GlobalPosition);
                GetTree().CurrentScene.AddChild(b);
                AudioManager.instance.PlaySFX("throw");
                camera.Zoom = new Vector2(cameraZoomDefault, cameraZoomDefault);
                countDown -= attackCountdownCost;
                throwTimer = throwTime;
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
