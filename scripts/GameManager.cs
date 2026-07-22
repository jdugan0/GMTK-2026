using System;
using System.Threading;
using Godot;

public partial class GameManager : Node
{
    public double countDown;
    public static GameManager instance;

    public override void _Ready()
    {
        instance = this;
    }

    public override void _Process(double delta)
    {
        countDown -= delta;
    }

    public void ApplyCountdownCost(double cost)
    {
        countDown -= cost;
    }
}
