using Godot;
using System;

public partial class Exit : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnCollide;
    }

	public void OnCollide(Node2D body)
	{
		if (body is Movement player)
		{
			GameManager.instance.Die(player);
		}
	}

}
