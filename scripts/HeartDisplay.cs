using Godot;

public partial class HeartDisplay : TextureRect
{
    private const int CellWidth = 217;
    private const int CellHeight = 291;

    [Export]
    private Texture2D sheet;

    [Export]
    private float frameTimeNormal = 0.35f;

    [Export]
    private float frameTimeWalk;

    [Export]
    private float frameTimeSprint;
    float frameTime;

    [Export]
    private float pulseScale = 1.6f;

    [Export]
    private float pulseDuration = 0.4f;

    private int _mode;

    [Export]
    public int mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
                return;
            _mode = value;
            Refresh();
            SpawnPulse();
        }
    }

    private readonly AtlasTexture atlas = new();
    private int frame;
    private double timer;

    public override void _Ready()
    {
        atlas.Atlas = sheet;
        Texture = atlas;
        Refresh();
    }

    public override void _Process(double delta)
    {
        timer += delta;
        if (timer < frameTime)
            return;

        timer = 0;
        frame ^= 1;
        Refresh();
    }

    public void Beat(int time)
    {
        switch (time)
        {
            case 0:
                frameTime = frameTimeNormal;
                break;
            case 1:
                frameTime = frameTimeWalk;
                break;
            case 2:
                frameTime = frameTimeSprint;
                break;
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

    private void Refresh() =>
        atlas.Region = new Rect2(frame * CellWidth, mode * CellHeight, CellWidth, CellHeight);
}
