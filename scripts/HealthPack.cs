using System;
using Godot;

public partial class HealthPack : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnCollide;
    }

    public void OnCollide(Node2D body)
    {
        if (body is Movement m)
        {
            m.countDown += 15;
            m.Gain(10);
			QueueFree();
			AudioManager.instance.PlaySFX("healthPickup");
        }
    }
}
