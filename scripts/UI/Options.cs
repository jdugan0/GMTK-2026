using System;
using Godot;

public partial class Options : Node
{
    [Export]
    Slider volume;

    [Export]
    Button back;

    public override void _Ready()
    {
        back.Pressed += MainMenu;
    }

    public void MainMenu()
    {
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("mainMenu", 1f);
    }
}
