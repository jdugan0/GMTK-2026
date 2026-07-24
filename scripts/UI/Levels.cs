using System;
using Godot;

public partial class Levels : Node
{
    [Export]
    TextureButton[] levelButtons;

    [Export]
    Button back;

    public override void _Ready()
    {
        back.Pressed += MainMenu;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i;
            levelButtons[i].Pressed += () =>
            {
                LevelManager.instance.LoadLevel(index);
            };
        }
    }

    public void MainMenu()
    {
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("mainMenu", 1f);
    }
}
