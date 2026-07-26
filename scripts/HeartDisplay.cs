using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class HeartDisplay : TextureRect
{
    private const int CellWidth = 217;
    private const int CellHeight = 291;

    private const int ExplodeFrames = 6;
    private const int ExplodeCellWidth = 1920;
    private const int ExplodeCellHeight = 1080;

    [Export]
    private Texture2D sheet;

    [Export]
    private float frameTimeNormal = 0.35f;
    public bool beat = true;

    [Export]
    private float frameTimeWalk;

    [Export]
    private float frameTimeSprint;

    [Export]
    private float contractedTime = 0.27f;

    [Export]
    private float pulseScale = 1.6f;

    [Export]
    private float pulseDuration = 0.4f;

    [Export]
    Texture2D explodeSheet;

    [Export]
    private float modeSfxFadeOut = 0.25f;

    private static readonly Dictionary<int, string[]> modeSfx = new()
    {
        [2] = new[] { "thirdHealthLost" },
        [3] = new[] { "gameOver" },
        [4] = new[] { "thirdHealthRemaining", "lowHealthClock" },
    };

    private int _mode;

    [Export]
    public int mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
                return;
            int previous = _mode;
            _mode = value;
            Refresh();
            SpawnPulse();
            UpdateModeSfx(previous);
        }
    }

    private readonly AtlasTexture atlas = new();
    private int frame;
    private double timer;
    private float restingTime;

    public override void _Ready()
    {
        atlas.Atlas = sheet;
        Texture = atlas;
        restingTime = frameTimeNormal;
        Refresh();
    }

    public override void _Process(double delta)
    {
        timer += delta;

        if (!beat)
            return;

        double hold = frame == 0 ? restingTime : contractedTime;
        if (timer < hold)
            return;

        timer = Mathf.Min(timer - hold, delta);
        frame = 1 - frame;
        if (frame == 1)
            AudioManager.instance.PlaySFX("heartbeat");
        Refresh();
    }

    public void Beat(int time)
    {
        restingTime = time switch
        {
            1 => frameTimeWalk,
            2 => frameTimeSprint,
            _ => frameTimeNormal,
        };
    }

    public async Task PlayExplode(float frameRate)
    {
        AudioManager.instance.PlaySFX("heartExplode");
        SetProcess(false);
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepAspectCentered;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var explodeAtlas = new AtlasTexture
        {
            Atlas = explodeSheet,
            Region = new Rect2(0, 0, ExplodeCellWidth, ExplodeCellHeight),
        };
        Texture = explodeAtlas;

        double frameTime = 1.0 / frameRate;
        for (int i = 0; i < ExplodeFrames; i++)
        {
            explodeAtlas.Region = new Rect2(
                0,
                i * ExplodeCellHeight,
                ExplodeCellWidth,
                ExplodeCellHeight
            );
            await ToSignal(GetTree().CreateTimer(frameTime), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private void SpawnPulse()
    {
        if (!IsInsideTree())
            return;

        var ghost = new TextureRect
        {
            Texture = new AtlasTexture { Atlas = sheet, Region = atlas.Region },
            Size = Size,
            PivotOffset = Size / 2,
            StretchMode = StretchMode,
            ExpandMode = ExpandMode,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(ghost);

        var tween = ghost.CreateTween().SetParallel();
        tween
            .TweenProperty(ghost, "scale", Vector2.One * pulseScale, pulseDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ghost, "modulate:a", 0f, pulseDuration);
        tween.Chain().TweenCallback(Callable.From(ghost.QueueFree));
    }

    private void UpdateModeSfx(int previous)
    {
        if (LevelManager.instance.currLevel == 6)
            return;
        if (modeSfx.TryGetValue(previous, out var stopping))
            foreach (string sound in stopping)
                AudioManager.instance.CancelSFXFadeOut(sound, modeSfxFadeOut);

        if (modeSfx.TryGetValue(_mode, out var starting))
            foreach (string sound in starting)
                AudioManager.instance.PlaySFX(sound);
    }

    private void Refresh() =>
        atlas.Region = new Rect2(frame * CellWidth, mode * CellHeight, CellWidth, CellHeight);
}
