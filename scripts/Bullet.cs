using System;
using Godot;

public partial class Bullet() : Area2D
{
    float attackDamage,
        attackRange,
        attackSpeed;
    Vector2 direction;

    public void Construct(
        float attackDamage,
        float attackRange,
        float attackSpeed,
        Vector2 direction,
        Vector2 position
    )
    {
        this.attackDamage = attackDamage;
        this.attackSpeed = attackSpeed;
        this.attackRange = attackRange;
        this.direction = direction;
        GlobalPosition = position;
    }

    public override void _Process(double delta)
    {
        var d = direction.Normalized();
        GlobalPosition += direction * attackSpeed * (float)delta;
    }

    public void OnCollide(Node2D body)
    {
        if (body.IsInGroup("destroy_bullet"))
        {
            QueueFree();
        }
    }
}
