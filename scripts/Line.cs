using System;
using Godot;

[GlobalClass]
public partial class Line : Resource
{
    [Export]
    public string line;

    [Export]
    public int emotion;
}
