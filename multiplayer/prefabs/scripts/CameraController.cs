using Godot;
using System;

public class CameraController : Camera
{
	private bool captured = true;
	private float sensibility = 0.2f;
	[Export] float shakeTime;
	[Export] float shakeForce;
	Spatial character;
	Spatial head;
	public override void _Ready()
	{
		head = (Spatial)GetParent().GetParent();
		character = (Spatial)head.GetParent();
	}

	public override void _Process(float delta)
	{
		shake(delta);
	}

	private void shake(float delta)
	{
		if (shakeTime > 0)
		{
			this.HOffset = (float)GD.RandRange(-shakeForce, shakeForce);
			this.VOffset = (float)GD.RandRange(-shakeForce, shakeForce);
			shakeTime -= delta;
		}
		else
		{
			this.HOffset = 0;
			this.VOffset = 0;
		}
	}

}
