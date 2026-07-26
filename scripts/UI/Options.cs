using System;
using Godot;

public partial class Options : Node
{
    [Export]
    Slider volume;

    [Export]
    Button back;

    [Export]
    Button hardToggle;

    public override void _Ready()
    {
        back.Pressed += MainMenu;

        hardToggle.Toggled += ToggleHard;

        volume.MinValue = 0;
        volume.MaxValue = 1;
        volume.Step = 0.01;
        volume.Value = AudioManager.instance.MasterVolume;
        volume.ValueChanged += v => AudioManager.instance.MasterVolume = (float)v;
        volume.DragEnded += _ => AudioManager.instance.SaveSettings();
    }

    public void ToggleHard(bool on)
    {
        AudioManager.instance.PlaySFX("textBox");
        OptionsManager.hardMode = on;
    }

    public void MainMenu()
    {
        AudioManager.instance.PlaySFX("textBox");
        AudioManager.instance.SaveSettings();
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("mainMenu", 1f);
    }
}
