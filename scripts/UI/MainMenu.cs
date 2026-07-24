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
        AudioManager.instance.PlaySFX("titleScreen");
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
        AudioManager.instance.CancelSFXFadeOut("titleScreen", 2f);
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("levelTest", 1f);
    }

    public void Options()
    {
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("options");
    }
}
