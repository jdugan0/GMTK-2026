using System;
using Godot;

public partial class Levels : Node
{
    [Export]
    TextureButton[] levelButtons;
    [Export]
    Texture2D lockedTexture;

    [Export]
    Button back;

    public override void _Ready()
    {
        back.Pressed += MainMenu;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i;
            if (i <= LevelManager.instance.unlockedLevel)
            {
                levelButtons[i].Pressed += () =>
                {
                    LevelManager.instance.LoadLevel(index);
                };
            }
            else
            {
                levelButtons[i].TextureNormal = lockedTexture;
                levelButtons[i].TextureHover = lockedTexture;
            }
        }
    }

    public void MainMenu()
    {
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("mainMenu", 1f);
    }
}
