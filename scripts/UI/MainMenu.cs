using System;
using Godot;

public partial class MainMenu : Node
{
    [Export]
    Button playButton;

    [Export]
    Button optionsButton;

    [Export]
    Button quitButton;

    [Export]
    Control flag;

    public override void _Ready()
    {
        MusicManager.instance.PlaySong("titleScreen");
        playButton.Pressed += Levels;
        optionsButton.Pressed += Options;
        quitButton.Pressed += Quit;
        if (LevelManager.instance.unlockedLevel >= 6)
        {
            flag.Visible = true;
        }
    }

    public void Quit()
    {
        GetTree().Quit();
    }

    public void Levels()
    {
        AudioManager.instance.PlaySFX("textBox");
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("levels", 1f);
    }

    public void Options()
    {
        AudioManager.instance.PlaySFX("textBox");
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("options", 1f);
    }
}
