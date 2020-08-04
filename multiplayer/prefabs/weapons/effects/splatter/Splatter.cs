using Godot;
using System;

public class Splatter : Particles
{

	[Export]
	NodePath timerPath;

	Timer timer;

	public override void _Ready()
	{
		timer = (Timer)GetNode(timerPath);
		timer.Connect("timeout", this, "queue_free");
	}
}
