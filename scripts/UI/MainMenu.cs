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

    public override void _Ready()
    {
        MusicManager.instance.PlaySong("titleScreen");
        playButton.Pressed += Play;
        optionsButton.Pressed += Options;
        quitButton.Pressed += Quit;
    }

    public void Quit()
    {
        GetTree().Quit();
    }

    public void Play()
    {
        MusicManager.instance.CancelSong(2f);
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("levelTest", 1f);
    }

    public void Options()
    {
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("options", 1f);
    }
}
