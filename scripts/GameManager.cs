using System;
using System.Security.Cryptography;
using System.Threading;
using Godot;

public partial class GameManager : Node
{
    public static GameManager instance;

    [Export]
    float combatExitDelay = 5f;

    public bool InCombat { get; private set; } = false;
    bool reported = false;
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

    public void Die(Movement player)
    {
        ui.Reset();
        player.Reset();
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide(LevelManager.instance.GetCurrLevel(), 1f);
        AudioManager.instance.PlaySFX("playerDies");
        // player.Visible = false;
        MusicManager.instance.CancelSong();
    }

    public void Win(Movement player)
    {
        MusicManager.instance.CancelSong();
        AudioManager.instance.CancelSFX("gameOver");
        MusicManager.instance.PlaySong("levelWin");
        ui.ShowWin();
        player.arrow.Visible = false;
    }

    public void ReportCombat()
    {
        reported = true;
    }

    public override void _PhysicsProcess(double delta)
    {
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
