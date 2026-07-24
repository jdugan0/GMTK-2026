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

    [Export]
    Control levelWon;

    [Export]
    Button winMainMenu;

    [Export]
    Button pauseMainMenu;

    [Export]
    Button resume;

    [Export]
    Button restart;

    [Export]
    Control pauseMenu;

    [Export]
    Button nextLevel;

    public bool IsPaused { get; private set; }

    public override void _Ready()
    {
        pauseMenu.Visible = false;
        levelWon.Visible = false;
        winMainMenu.Pressed += MainMenu;
        pauseMainMenu.Pressed += MainMenu;
        restart.Pressed += Restart;
        resume.Pressed += () => SetPaused(false);
        nextLevel.Pressed += LevelManager.instance.NextLevel;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("PAUSE"))
            return;
        if (!IsPaused && levelWon.Visible)
            return;
        SetPaused(!IsPaused);
        GetViewport().SetInputAsHandled();
    }

    public void Pause()
    {
        SetPaused(true);
    }

    private void SetPaused(bool value)
    {
        IsPaused = value;
        pauseMenu.Visible = value;
        GetTree().Paused = value;
        UpdateTutorialVisibility();
    }

    public void ShowWin()
    {
        IsPaused = false;
        pauseMenu.Visible = false;
        levelWon.Visible = true;
        GetTree().Paused = true;
        UpdateTutorialVisibility();
    }

    public void Restart()
    {
        CloseMenus();
        GameManager.instance.Die(player);
    }

    public void MainMenu()
    {
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("mainMenu", 1f);
        MusicManager.instance.CancelSong(1f);
        AudioManager.instance.CancelAllSFX();
    }

    private void CloseMenus()
    {
        IsPaused = false;
        pauseMenu.Visible = false;
        levelWon.Visible = false;
        GetTree().Paused = true;
        UpdateTutorialVisibility();
    }

    private void UpdateTutorialVisibility()
    {
        if (TutorialManager.instance != null)
            TutorialManager.instance.Visible = !(pauseMenu.Visible || levelWon.Visible);
    }

    public void Reset()
    {
        ((ShaderMaterial)vignette.Material).SetShaderParameter("intensity", 0);
    }

    public override void _PhysicsProcess(double delta)
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
