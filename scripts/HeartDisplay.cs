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
    public int mode;

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

    private void Refresh() =>
        atlas.Region = new Rect2(frame * CellWidth, mode * CellHeight, CellWidth, CellHeight);
}
