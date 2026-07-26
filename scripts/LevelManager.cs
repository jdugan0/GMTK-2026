using System;
using Godot;

public partial class LevelManager : Node
{
    [Export]
    public string[] levels;
    public int currLevel;

    [Export]
    public int unlockedLevel = 0;

    public int unlockedLevelHard = 0;

    public static LevelManager instance;

    const string SavePath = "user://save.cfg";
    const string SaveSection = "progress";
    const string SaveKey = "unlocked_level";
    const string HardModeKey = "unlocked_level_hard";

    public int UnlockedLevel
    {
        get => OptionsManager.hardMode ? unlockedLevelHard : unlockedLevel;
        private set
        {
            if (OptionsManager.hardMode)
                unlockedLevelHard = value;
            else
                unlockedLevel = value;
        }
    }

    public override void _Ready()
    {
        instance = this;
        LoadProgress();
    }

    public void UnlockLevel(int level)
    {
        if (level < UnlockedLevel || level >= levels.Length)
            return;

        UnlockedLevel = level;
        SaveProgress();
    }

    void LoadProgress()
    {
        ConfigFile config = new ConfigFile();
        if (config.Load(SavePath) != Error.Ok)
            return;
        int saved = (int)config.GetValue(SaveSection, SaveKey, unlockedLevel);
        unlockedLevel = Mathf.Clamp(Mathf.Max(saved, unlockedLevel), 0, levels.Length - 1);
        int savedHard = (int)config.GetValue(SaveSection, HardModeKey, unlockedLevelHard);
        unlockedLevelHard = Mathf.Clamp(Mathf.Max(savedHard, unlockedLevelHard), 0, levels.Length - 1);
    }

    void SaveProgress()
    {
        ConfigFile config = new ConfigFile();
        config.Load(SavePath);
        config.SetValue(SaveSection, SaveKey, unlockedLevel);
        config.SetValue(SaveSection, HardModeKey, unlockedLevelHard);
        config.Save(SavePath);
    }

    public string GetCurrLevel()
    {
        return levels[currLevel];
    }

    public void LoadLevel(int level)
    {
        AudioManager.instance.PlaySFX("textBox");
        GD.Print(level);
        currLevel = level;
        _ = SceneSwitcher.instance.SwitchSceneAsyncSlide(levels[level], 1f);
        MusicManager.instance.CancelSong(2f);
    }

    public void NextLevel()
    {
        AudioManager.instance.PlaySFX("textBox");
        LoadLevel(currLevel + 1);
    }
}
