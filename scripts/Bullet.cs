using System;
using Godot;

public partial class Bullet() : Area2D
{
    float knockback,
        stun,
        speed;
    Vector2 direction;

    public void Construct(
        float knockback,
        float stun,
        float speed,
        Vector2 direction,
        Vector2 position
    )
    {
        this.knockback = knockback;
        this.stun = stun;
        this.speed = speed;
        this.direction = direction.Normalized();
        GlobalPosition = position;
    }

    public override void _Process(double delta)
    {
        GlobalPosition += direction * speed * (float)delta;
    }

    public override void _Ready()
    {
        GetChild<AnimatedSprite2D>(0).Play();
        BodyEntered += OnCollide;
        AreaEntered += OnCollide;
    }

    public void OnCollide(Node2D body)
    {
        if (body is Area2D)
        {
            OnCollide(body.GetParent<Node2D>());
            return;
        }
        if (body is Enemy e)
        {
            AudioManager.instance.PlaySFX("limbHit");
            QueueFree();
            e.Shove(direction, knockback, stun);
        }
        if (body.IsInGroup("destroy_bullet"))
        {
            AudioManager.instance.PlaySFX("limbHit");
            QueueFree();
        }
    }
}
