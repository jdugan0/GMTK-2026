using System;
using Godot;

public partial class TutorialManager : CanvasLayer
{
    public static TutorialManager instance;
    public TutorialPopup curr;

    public override void _Ready()
    {
        instance = this;
    }
}
