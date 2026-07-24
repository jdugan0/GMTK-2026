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

    public override void _Ready()
    {
        instance = this;
        AudioManager.instance.PlaySFX("outOfCombatBackground");
        randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
    }

    public void Die(Movement player)
    {
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide("level_test");
        AudioManager.instance.PlaySFX("playerDies");
        // player.Visible = false;
        if (InCombat)
        {
            AudioManager.instance.CancelSFXFadeOut("combat", 4.0f).p.Finished += () =>
                AudioManager.instance.PlaySFX("outOfCombatBackground", time);
        }
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
                AudioStreamPlayer p = AudioManager.instance.CancelSFX("outOfCombatBackground").p;
                if (p != null)
                {
                    time = p.GetPlaybackPosition();
                }
                AudioManager.instance.PlaySFX("combat");
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
                AudioManager.instance.CancelSFXFadeOut("combat", 4.0f).p.Finished += () =>
                    AudioManager.instance.PlaySFX("outOfCombatBackground", time);
            }
        }
    }
}
