using System;
using Godot;

public partial class Bullet() : Area2D
{
    float knockback,
        stun,
        speed;
    Vector2 direction;

    [Export]
    PackedScene bloodSplat;

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

    public void Splat()
    {
        var b = bloodSplat.Instantiate<GpuParticles2D>();
        GetTree().CurrentScene.AddChild(b);
        b.Restart();
        b.GlobalPosition = GlobalPosition;
        b.Emitting = true;
        b.Finished += b.QueueFree;
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
            Splat();
            AudioManager.instance.PlaySFX("limbHit");
            QueueFree();
            e.Shove(direction, knockback, stun);
        }
        if (body.IsInGroup("destroy_bullet"))
        {
            Splat();
            AudioManager.instance.PlaySFX("limbHit");
            QueueFree();
        }
        if (body.IsInGroup("crate"))
        {
            Crate c = (Crate)body;
            Splat();
            AudioManager.instance.PlaySFX("boxHit");
            c.Destroy();
            QueueFree();
            body.QueueFree();
        }
    }
}
