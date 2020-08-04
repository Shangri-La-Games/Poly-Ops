using Godot;
using System;

public class Weapons : Spatial
{
	[Export]
	NodePath PlayerNode;
	Player player;

	[Export]
	NodePath headPath;
	private Spatial head;

	[Export]
	NodePath crosshairPath;

	PackedScene decalScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/weapons/effects/decal/decal.tscn");
	private Godot.Collections.Dictionary<String, Weapon> arsenal = new Godot.Collections.Dictionary<string, Weapon>();
	private String currentWeapon = "desert_eagle";

	float timer = 0;
	float fireRate = 1f;

	public override void _Ready()
	{

		SetAsToplevel(true);

		// Get nodes
		player = (Player)GetNode(PlayerNode);
		head = (Spatial)GetNode(headPath);
		Camera camera = (Camera)player.getCamera();

		Control crosshair = (Control)GetNode(crosshairPath);

		// Create weapon for all 	arsenals
		arsenal["desert_eagle"] = new Weapon(this, camera, "desert_eagle", crosshair, 9, 9, 10, 100);
		arsenal["ak47"] = new Weapon(this, camera, "ak47", crosshair, 30, 30, 20, 100);

		foreach (Weapon weapon in arsenal.Values) { weapon.hide(); }

		switchWeapon();
	}

	public override void _PhysicsProcess(float delta)
	{

		timer += delta;

		weapon(delta);

		if (player.input["switch"] == 1)
		{
			if (currentWeapon == "desert_eagle")
			{
				currentWeapon = "ak47";
				fireRate = 0.1f;
			}
			else
			{
				currentWeapon = "desert_eagle";
				fireRate = 0.5f;
			}
			switchWeapon();
		}

		position(delta);
	}

	private void weapon(float delta)
	{
		// Selected weapon
		Weapon weapon = arsenal[currentWeapon];

		Vector3 PlayerDirection = (Vector3)player.Get("direction");

		if (player.input["sprint"] != 1 || PlayerDirection != Vector3.Zero)
		{
			if (player.input["shoot"] == 1)
			{
				// temproray soln
				if (timer > fireRate)
				{
					timer = 0;
					weapon.shoot(delta, player.isPuppetController());
				}
			}

			// Update weapon focus
			weapon.focus(player.input["focus"], delta);
		}

		// Reload
		if (player.input["reload"] == 1) { weapon.reload(); }

		// Update arsenal
		foreach (Weapon w in arsenal.Values) { w.update(delta); }
	}

	private void switchWeapon()
	{
		foreach (Weapon w in arsenal.Values)
		{
			if (w != arsenal[currentWeapon]) { w.hide(); }
			else { w.draw(); }
		}
	}
	private void position(float delta)
	{
		float yLerp = 20;
		float xLerp = 40;

		this.GlobalTransform = new Transform(
			this.GlobalTransform.basis,
			this.head.GlobalTransform.origin
		);

		Vector3 headBasis = head.GlobalTransform.basis.GetEuler();
		if (player.input["focus"] != 1)
		{
			this.Rotation = new Vector3(
				Mathf.LerpAngle(Rotation.x, headBasis.x, yLerp * delta),
				Mathf.LerpAngle(Rotation.y, headBasis.y, xLerp * delta),
				this.Rotation.z
			);
		}
		else { Rotation = headBasis; }
	}
}
