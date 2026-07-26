using System;
using Godot;

public partial class Advisory : Node
{
    [Export]
    float warnTime;
    float warnTimer;
    bool switchTime = false;

    public override void _Ready()
    {
        warnTimer = warnTime;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ATTACK"))
        {
            switchTime = true;
            _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("mainMenu", 1f);
            return;
        }
        warnTimer -= (float)delta;
        if (warnTimer <= 0 && !switchTime)
        {
            switchTime = true;
            _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("mainMenu", 1f);
        }
    }
}
