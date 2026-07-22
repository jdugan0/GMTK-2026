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
        if (!CanSeePlayer())
        {
            GD.Print("No plater");
            return;
        }
        if (NavigationServer2D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0)
        {
            GD.Print("no map");
            return;
        }

        SetMovementTarget(player.GlobalPosition);

        if (_navigationAgent.IsNavigationFinished())
        {
            return;
        }
        GD.Print("RAHH");
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
