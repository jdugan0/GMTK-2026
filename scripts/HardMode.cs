using System;
using Godot;

public partial class HardMode : Node
{
    [Export]
    public bool destroyOnHard;

    public override void _Ready()
    {
        if (OptionsManager.hardMode && destroyOnHard)
        {
            QueueFree();
        }
        if (!OptionsManager.hardMode && !destroyOnHard)
        {
            QueueFree();
        }
    }
}
