using System;
using Godot;

public partial class TutorialPopup : Control
{
	[Export]
	Area2D area;

	[Export]
	float time;

	float timer;

	public override void _Ready()
	{
		area.BodyEntered += Trigger;
		timer = time;
	}

	public override void _Process(double delta)
	{
		if (Visible)
		{
			timer -= (float)delta;
		}
		if (timer <= 0)
		{
			TutorialManager.instance.curr = null;
			QueueFree();
		}
	}

	public void Trigger()
	{
		if (TutorialManager.instance.curr != null)
		{
			TutorialManager.instance.curr.End();
		}
		TutorialManager.instance.curr = this;
		Visible = true;
	}

	public void Trigger(Node2D body)
	{
		if (body is Movement)
		{
			Trigger();
		}
	}

	public void End()
	{
		QueueFree();
		TutorialManager.instance.curr = null;
	}
}
