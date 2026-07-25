using System;
using System.Security.Cryptography;
using System.Threading;
using Godot;

public partial class GameManager : Node
{
    public static GameManager instance;

    [Export]
    float combatExitDelay = 5f;

    [Export]
    float deathTime;

    public bool InCombat { get; private set; } = false;
    bool reported = false;
    bool dying = false;
    float combatExitTimer = 0f;
    float time = 0;
    float randomSoundTimer;

    [Export]
    UI ui;

    public override void _Ready()
    {
        instance = this;
        MusicManager.instance.PlaySong("outOfCombatBackground");
        randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
    }

    public async void Die(Movement player)
    {
        if (dying)
            return;
        dying = true;
        ui.Reset();
        player.Reset();
        AudioManager.instance.CancelAllSFX();
        AudioManager.instance.PlaySFX("playerDies");
        foreach (var item in GetTree().GetNodesInGroup("bullet"))
        {
            ((Node2D)item).Visible = false;
        }
        await SceneSwitcher.instance.WaitOneFrame();
        player.sprite2D.Play("DEATH");
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        ui.HeartAnim();
        GetTree().Paused = true;
        await ToSignal(GetTree().CreateTimer(2f), SceneTreeTimer.SignalName.Timeout);
        await SceneSwitcher.instance.SwitchSceneAsyncSlide(
            LevelManager.instance.GetCurrLevel(),
            1f
        );
    }

    public void Win(Movement player)
    {
        // AudioManager.instance.CancelAllSFX();
        // MusicManager.instance.PlaySong("levelWin");
        ui.ShowWin();
        player.arrow.Visible = false;
    }

    public void ReportCombat()
    {
        reported = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (dying)
            return;

        bool rawInCombat = reported;
        reported = false;

        if (!InCombat)
        {
            randomSoundTimer -= (float)delta;
        }
        if (randomSoundTimer <= 0)
        {
            randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
            AudioManager.instance.PlaySFX("outOfCombatRandom");
        }

        if (rawInCombat)
        {
            combatExitTimer = 0f;
            if (!InCombat)
            {
                InCombat = true;
                GD.Print("IN COMBAT");
                AudioStreamPlayer p = MusicManager.instance.CancelSong().p;
                if (p != null)
                {
                    time = p.GetPlaybackPosition();
                }
                MusicManager.instance.PlaySong("combat");
            }
        }
        else if (InCombat)
        {
            combatExitTimer += (float)delta;
            if (combatExitTimer >= combatExitDelay)
            {
                InCombat = false;
                combatExitTimer = 0f;
                randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
                GD.Print("OUT COMBAT");
                AudioStreamPlayer p = MusicManager.instance.CancelSong(4.0f).p;
                if (p != null)
                    p.Finished += () =>
                        MusicManager.instance.PlaySong("outOfCombatBackground", time);
            }
        }
    }
}
