using System;
using System.Security.Cryptography;
using System.Threading;
using Godot;

public partial class GameManager : Node
{
    public static GameManager instance;

    public bool InCombat { get; private set; } = false;
    bool reported = false;
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
        player.Visible = false;
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
        if (!InCombat)
        {
            randomSoundTimer -= (float)delta;
        }
        if (randomSoundTimer <= 0)
        {
            randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
            AudioManager.instance.PlaySFX("outOfCombatRandom");
        }
        if (reported != InCombat)
        {
            if (InCombat)
            {
                randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
                GD.Print("OUT COMBAT");
                AudioManager.instance.CancelSFXFadeOut("combat", 4.0f).p.Finished += () =>
                    AudioManager.instance.PlaySFX("outOfCombatBackground", time);
            }
            else
            {
                GD.Print("IN COMBAT");
                AudioStreamPlayer p = AudioManager.instance.CancelSFX("outOfCombatBackground").p;
                if (p != null)
                {
                    time = p.GetPlaybackPosition();
                }
                AudioManager.instance.PlaySFX("combat");
            }
        }
        InCombat = reported;
        reported = false;
    }
}
