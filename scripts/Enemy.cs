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
    private float MovementSpeed { get; set; } = 4.0f;

    [Export]
    private NavigationAgent2D _navigationAgent;

    [Export]
    private float attackDistance;

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

    public override void _Ready()
    {
        player = (Movement)GetTree().GetFirstNodeInGroup("player");
        _navigationAgent.VelocityComputed += OnVelocityComputed;
    }

    private void SetMovementTarget(Vector2 movementTarget)
    {
        _navigationAgent.TargetPosition = movementTarget;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        if (attackTimer > 0)
        {
            attackTimer -= dt;
        }

        if (attacking)
        {
            if (attackTimer <= 0)
            {
                if (player.GlobalPosition.DistanceTo(GlobalPosition) <= attackRange)
                {
                    player.Attack(attackDamage, this);
                    attacking = false;
                    attackTimer = attackCooldown;
                    stunTimer = attackStun;
                }
            }
            return;
        }
        if (stunTimer > 0)
        {
            stunTimer -= dt;
            return;
        }
        if (!CanSeePlayer())
        {
            return;
        }
        if (NavigationServer2D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0)
        {
            return;
        }
        GD.Print(player.GlobalPosition.DistanceTo(GlobalPosition));
        if (player.GlobalPosition.DistanceTo(GlobalPosition) < attackDistance)
        {
            if (attackTimer <= 0)
            {
                attackTimer = attackDelay;
                attacking = true;
            }
            return;
        }

        SetMovementTarget(player.GlobalPosition);

        if (_navigationAgent.IsNavigationFinished())
        {
            if (attackTimer <= 0)
            {
                attackTimer = attackDelay;
                attacking = true;
            }
            return;
        }
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
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        Godot.Collections.Dictionary result = spaceState.IntersectRay(query);
        if (result.Count == 0)
            return false;

        return (Node)result["collider"] == player;
    }
}
