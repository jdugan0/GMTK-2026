using Godot;

public partial class HeartDisplay : TextureRect
{
    private const int CellWidth = 217;
    private const int CellHeight = 291;

    [Export]
    private Texture2D sheet;

    [Export]
    private float frameTime = 0.35f;

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

    private void Refresh() =>
        atlas.Region = new Rect2(frame * CellWidth, mode * CellHeight, CellWidth, CellHeight);
}
