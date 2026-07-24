using System;
using Godot;

public partial class LevelManager : Node
{
    [Export]
    public string[] levels;
    public int currLevel;

    public static LevelManager instance;

    public override void _Ready()
    {
        instance = this;
    }

    public void LoadLevel(int level)
    {
		GD.Print(level);
        currLevel = level;
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide(levels[level], 1f);
        MusicManager.instance.CancelSong(2f);
    }

    public void NextLevel()
    {
        LoadLevel(currLevel + 1);
    }
}
