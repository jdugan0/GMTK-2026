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

    [Export]
    PackedScene lossText;

    [Export]
    ColorRect vignette;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        double z = player.countDown / player.initalCountdown;
        ((ShaderMaterial)vignette.Material).SetShaderParameter("intensity", 1 - z);
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

    public void Loss(int amount)
    {
        var t = lossText.Instantiate<Label>();
        t.ZIndex = -1;
        t.Text = $"-{amount}";
        AddChild(t);
        t.Position = player.GetGlobalTransformWithCanvas().Origin;
        var tween = GetTree().CreateTween();
        tween
            .TweenProperty(t, "position", heartDisplay.GlobalPosition, 2f)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.InOut)
            .Finished += t.QueueFree;
    }

    public void Beat(int mode)
    {
        heartDisplay.Beat(mode);
    }
}
