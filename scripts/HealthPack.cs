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
            m.countDown += 10;
            m.Gain(10);
			QueueFree();
        }
    }
}
