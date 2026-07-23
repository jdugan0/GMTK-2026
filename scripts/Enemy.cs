using System;
using System.ComponentModel;
using System.Runtime.InteropServices.Marshalling;
using Godot;

public partial class Enemy : CharacterBody2D
{
    private Movement player = null;

    [Export]
    private float losDistance;

    [Export]
    private float leashDistance;

    [Export]
    private float knockbackFriction = 2000f;

    [Export]
    private float MovementSpeed { get; set; } = 4.0f;

    [Export]
    private NavigationAgent2D _navigationAgent;

    [Export]
    private float attackDistance;

    [Export]
    private AnimatedSprite2D sprite2D;

    [Export]
    Node2D flashlight;

    [ExportGroup("Attacking")]
    [Export]
    private float attackDamage;

    [Export]
    private float attackCooldown;

    [Export]
    private float attackDelay;

    [Export]
    private float attackStun;

    [Export]
    private float attackRange;

    private bool attacking = false;
    private float attackTimer = 0;
    private float stunTimer = 0;
    private bool spotted;
    private bool returningHome;
    private bool stunFromHit;

    private Vector2 homePosition;
    private Vector2 knockbackVelocity;

    private string _lastDebugState = "";

    private void DebugState(string state)
    {
        if (state == _lastDebugState)
            return;
        _lastDebugState = state;
        GD.Print(
            $"[{Name}] {state} | dist={player.GlobalPosition.DistanceTo(GlobalPosition):F0} "
                + $"attackTimer={attackTimer:F2} stunTimer={stunTimer:F2} "
                + $"attacking={attacking} spotted={spotted}"
        );
    }

    public override void _Ready()
    {
        player = (Movement)GetTree().GetFirstNodeInGroup("player");
        _navigationAgent.VelocityComputed += OnVelocityComputed;
        homePosition = GlobalPosition;
    }

    private void SetMovementTarget(Vector2 movementTarget)
    {
        _navigationAgent.TargetPosition = movementTarget;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (knockbackVelocity.LengthSquared() > 25f)
        {
            Velocity = knockbackVelocity;
            knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, knockbackFriction * dt);
            MoveAndSlide();
            UpdateAnimation(GlobalPosition.DirectionTo(player.GlobalPosition), true);
            DebugState("KNOCKED_BACK");
            return;
        }

        if (returningHome)
        {
            if (GlobalPosition.DistanceTo(homePosition) <= 30f)
                returningHome = false;
        }
        else
        {
            bool playerInZone = player.GlobalPosition.DistanceTo(homePosition) <= leashDistance;
            if (CanSeePlayer() && playerInZone)
            {
                spotted = true;
            }
            if (spotted && GlobalPosition.DistanceTo(homePosition) > leashDistance)
            {
                spotted = false;
                returningHome = true;
            }
        }
        if (spotted || attacking)
        {
            GameManager.instance.ReportCombat();
            UpdateAnimation(GlobalPosition.DirectionTo(player.GlobalPosition), false);
        }
        if (attackTimer > 0)
        {
            attackTimer -= dt;
        }

        if (attacking)
        {
            if (attackTimer <= 0)
            {
                attacking = false;
                if (player.GlobalPosition.DistanceTo(GlobalPosition) <= attackRange)
                {
                    DebugState("ATTACK_HIT");
                    player.Attack(attackDamage, this);
                    attackTimer = attackCooldown;
                    stunTimer = attackStun;
                    stunFromHit = false;
                }
                else
                {
                    DebugState("ATTACK_WHIFF (player left attackRange during windup)");
                }
            }
            else
            {
                DebugState("ATTACK_WINDUP");
            }
            return;
        }
        if (stunTimer > 0)
        {
            stunTimer -= dt;
            UpdateAnimation(GlobalPosition.DirectionTo(player.GlobalPosition), stunFromHit);
            DebugState("STUNNED");
            return;
        }
        if (!spotted)
        {
            if (GlobalPosition.DistanceTo(homePosition) > 20f)
            {
                SetMovementTarget(homePosition);
                if (!_navigationAgent.IsNavigationFinished())
                {
                    DebugState("RETURNING_HOME");
                    Vector2 homeStep = _navigationAgent.GetNextPathPosition();
                    Vector2 homeVel = GlobalPosition.DirectionTo(homeStep) * MovementSpeed;
                    if (_navigationAgent.AvoidanceEnabled)
                        _navigationAgent.Velocity = homeVel;
                    else
                        OnVelocityComputed(homeVel);
                    return;
                }
            }
            DebugState("IDLE (not spotted)");
            return;
        }
        if (NavigationServer2D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0)
        {
            DebugState("WAITING_FOR_NAVMAP");
            return;
        }
        if (player.GlobalPosition.DistanceTo(GlobalPosition) < attackDistance)
        {
            if (attackTimer <= 0)
            {
                DebugState("ATTACK_START (in attackDistance)");
                attackTimer = attackDelay;
                attacking = true;
            }
            else
            {
                DebugState("COOLDOWN_WAIT (in attackDistance, attackTimer running)");
            }
            return;
        }

        SetMovementTarget(player.GlobalPosition);

        if (_navigationAgent.IsNavigationFinished())
        {
            if (attackTimer <= 0 && player.GlobalPosition.DistanceTo(GlobalPosition) <= attackRange)
            {
                DebugState("ATTACK_START (nav finished)");
                attackTimer = attackDelay;
                attacking = true;
            }
            else
            {
                DebugState("NAV_FINISHED_STUCK (path ended, player out of reach)");
            }
            return;
        }
        DebugState("CHASING");
        Vector2 nextPathPosition = _navigationAgent.GetNextPathPosition();
        Vector2 newVelocity = GlobalPosition.DirectionTo(nextPathPosition) * MovementSpeed;
        if (_navigationAgent.AvoidanceEnabled)
        {
            _navigationAgent.Velocity = newVelocity;
        }
        else
        {
            OnVelocityComputed(newVelocity);
        }
    }

    private void UpdateAnimation(Vector2 lookDir, bool flinch)
    {
        flashlight.Rotation = lookDir.Angle();

        string anim;
        bool flip = false;
        if (Mathf.Abs(lookDir.X) > Mathf.Abs(lookDir.Y))
        {
            anim = "RIGHT";
            flip = lookDir.X < 0;
        }
        else
        {
            anim = lookDir.Y > 0 ? "DOWN" : "UP";
        }

        sprite2D.Animation = anim;
        sprite2D.FlipH = flip;
        sprite2D.Frame = flinch ? 1 : 0;
        sprite2D.Pause();
    }

    private void OnVelocityComputed(Vector2 safeVelocity)
    {
        Velocity = safeVelocity;
        MoveAndSlide();
    }

    private bool CanSeePlayer()
    {
        if (player.GlobalPosition.DistanceTo(GlobalPosition) > losDistance)
        {
            return false;
        }
        PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
            GlobalPosition,
            player.GlobalPosition
        );
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid(), GetChild<Area2D>(4).GetRid() };
        query.CollisionMask = 1 << 8;
        query.CollideWithAreas = true;

        Godot.Collections.Dictionary result = spaceState.IntersectRay(query);
        if (result.Count == 0)
            return false;
        return ((Node)result["collider"]).IsInGroup("player_col");
    }

    public void Shove(Vector2 dir, float knockback, float stunTime)
    {
        knockbackVelocity = dir.Normalized() * knockback;
        stunTimer = Mathf.Max(stunTimer, stunTime);
        stunFromHit = true;
        attacking = false;
        spotted = false;
        GD.Print($"[{Name}] SHOVE knockback={knockback:F0} stun={stunTime:F2}");
    }
}
