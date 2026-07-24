using System;
using Godot;

public partial class UI : CanvasLayer
{
    [Export]
    private Movement player;

    [Export]
    private Label countDownLabel;

    [Export]
    HeartDisplay heartDisplay;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        countDownLabel.Text = $"{Math.Round(player.countDown)}";

        if (player.countDown <= player.initalCountdown * (1f / 3f))
        {
            heartDisplay.mode = 2;
        }
        else if (player.countDown <= player.initalCountdown * (2f / 3f))
        {
            heartDisplay.mode = 1;
        }
        else
        {
            heartDisplay.mode = 0;
        }
    }

    public void Beat(int mode)
    {
        heartDisplay.Beat(mode);
    }
}
