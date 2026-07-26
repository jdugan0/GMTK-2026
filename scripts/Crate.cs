using System;
using Godot;

public partial class Crate : Node2D
{
	[Export]
    Texture2D destroy;

    [Export]
    AnimatedSprite2D cpy;

    public void Destroy()
    {
        var n = new Sprite2D();
        n.Texture = destroy;
        n.Scale = cpy.Scale;
        GetTree().CurrentScene.AddChild(n);
        n.GlobalPosition = cpy.GlobalPosition;
    }
}
