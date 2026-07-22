using System;
using Godot;

public partial class UI : CanvasLayer
{
    [Export]
    private Movement player;

    [Export]
    private Label countDownLabel;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        countDownLabel.Text = $"{Math.Round(player.countDown)}";
    }
}
