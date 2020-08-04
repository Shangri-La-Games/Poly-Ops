using Godot;
using System;

public class Decal : Spatial
{

	[Export]
	NodePath timerPath;

	public override void _Ready()
	{
		Timer timer = (Timer)GetNode(timerPath);
		timer.Connect("timeout", this, "queue_free");
	}

}
