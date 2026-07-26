using System;
using System.Transactions;
using Godot;

public partial class Cutscene : TextureRect
{
    [Export]
    Texture2D[] frames;

    [Export]
    float frameDelay;

    [Export]
    string soundToPlay;
    float frameTimer = 0;

    [Export]
    string nextScene;
    int frame = -1;
    bool switching = false;

    [Export]
    float startDelay;

    [Export]
    ColorRect initalFrame;

    public override void _Ready()
    {
        if (soundToPlay != null)
        {
            AudioManager.instance.PlaySFX(soundToPlay);
        }
        frameTimer = startDelay;
    }

    public override void _Process(double delta)
    {
        frameTimer -= (float)delta;
        if (switching)
        {
            return;
        }
        if (Input.IsActionJustPressed("ATTACK"))
        {
            switching = true;
            _ = SceneSwitcher.instance.SwitchSceneAsyncSlide(nextScene, 1f);
            AudioManager.instance.CancelAllSFX();
            return;
        }
        if (frameTimer <= 0 && !switching)
        {
            initalFrame.Visible = false;
            frame++;
            if (frame == frames.Length)
            {
                switching = true;
                _ = SceneSwitcher.instance.SwitchSceneAsyncSlide(nextScene, 1f);
                AudioManager.instance.CancelAllSFX();
                return;
            }
            Texture = frames[frame];
            frameTimer = frameDelay;
        }
    }
}
