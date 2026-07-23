using System;
using System.Threading;
using Godot;

public partial class GameManager : Node
{
    public static GameManager instance;

    public override void _Ready()
    {
        instance = this;
    }

    public override void _Process(double delta)
    {
    }
}
