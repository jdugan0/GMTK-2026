using System;
using Godot;

public partial class Exit : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnCollide;
    }

    public override void _Process(double delta)
    {
        // GD.Print(
        //     ((Movement)GetTree().GetFirstNodeInGroup("player")).GlobalPosition.DistanceTo(
        //         GlobalPosition
        //     )
        // );
        if (
            ((Movement)GetTree().GetFirstNodeInGroup("player")).GlobalPosition.DistanceTo(
                GlobalPosition
            ) < 1900
        )
        {
            if (AudioManager.instance.GetPlaying("exitHum").Count == 0)
            {
                AudioManager.instance.PlaySFX("exitHum");
            }
        }
        else
        {
            AudioManager.instance.CancelSFX("exitHum");
        }
    }

    public void OnCollide(Node2D body)
    {
        if (body is Movement player)
        {
            GameManager.instance.Win(player);
        }
    }
}
