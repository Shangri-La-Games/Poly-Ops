using Godot;
using System;

public class Weapon : Spatial
{
	int bullets;
	int maxBullets;
	int damage;
	int ammo;
	int lerpSpeed = 30;
	Spatial owner;
	String weaponName;
	// RayCast bulletRay;
	Control crosshair;
	Spatial weaponNode;
	Camera camera;
	PackedScene decalScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/weapons/effects/decal/decal.tscn");
	PackedScene sparkScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/weapons/effects/spark/spark.tscn");
	PackedScene splatterScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/weapons/effects/splatter/splatter.tscn");
	PackedScene muzzleScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/weapons/effects/muzzle/muzzle.tscn");
	AnimationPlayer animation;
	// Sound
	Node audioNode;
	Position3D barrelNode;

	public Weapon(Spatial owner, Camera camera, String weaponName, Control crosshair, int bullets, int maxBullets, int damage, int ammo)
	{
		this.owner = owner;
		this.camera = camera;
		this.weaponName = weaponName;
		this.crosshair = crosshair;
		this.bullets = bullets;
		this.maxBullets = maxBullets;
		this.damage = damage;
		this.ammo = ammo;

		// this.bulletRay = (RayCast)camera.GetNode("bullet_ray");
		this.weaponNode = (Spatial)owner.GetNode(weaponName);
		this.audioNode = owner.GetNode(String.Format("{0}/audio", weaponName));
		this.barrelNode = (Position3D)owner.GetNode(String.Format("{0}/barrel", weaponName));

		this.animation = (AnimationPlayer)owner.GetNode(String.Format("{0}/anim", weaponName));
		animation.Connect("animation_finished", this, "animationTimeout");
	}
	public void draw()
	{
		if (!weaponNode.Visible)
		{
			weaponNode.Show();

			// Audio.
			AudioStreamPlayer3D draw = ((AudioStreamPlayer3D)audioNode.GetNode("draw"));
			draw.PitchScale = (float)GD.RandRange(0.9, 1.1);
			draw.Play();
		}

		// Notify weapon update
		GameState.instance.EmitSignal(nameof(GameState.updateWeapon), weaponName, bullets, ammo);
	}
	public void hide() { if (weaponNode.Visible) { weaponNode.Hide(); } }
	public void sprint(int sprint, float delta) { }

	public void shoot(float delta, bool isPuppet)
	{
		if (reloadWeapon) { return; }
		if (!isPuppet) { return; }

		// Get bullet ray.
		RayCast bulletRay = (RayCast)camera.GetNode("bullet_ray");

		if (bullets > 0)
		{
			bullets -= 1;

			// Notify bullet update
			GameState.instance.EmitSignal(nameof(GameState.updateWeapon), weaponName, bullets, ammo);

			if (camera != null)
			{
				// Recoil
				camera.Rotation = new Vector3(
					Mathf.Lerp(camera.Rotation.x, (float)GD.RandRange(1, 2), delta),
					Mathf.Lerp(camera.Rotation.y, (float)GD.RandRange(-1, 1), delta),
					camera.Rotation.z
				);

				// Shake
				camera.Set("shakeForce", 0.002);
				camera.Set("shakeTime", 0.2);
			}

			// Audio
			AudioStreamPlayer3D shoot = ((AudioStreamPlayer3D)audioNode.GetNode("shoot"));
			shoot.PitchScale = (float)GD.RandRange(0.9, 1.1);
			shoot.Play();

			// Update crosshair
			crosshair.RectScale = crosshair.RectScale.LinearInterpolate(new Vector2(4, 4), 10 * delta);

			if (bulletRay.IsColliding())
			{
				// Object collision
				CollisionObject collisionObject = (CollisionObject)bulletRay.GetCollider();

				// Add spark to props.
				if (collisionObject.IsInGroup("props"))
				{
					// Add Spark
					Particles spark = (Particles)sparkScene.Instance();
					((Node)bulletRay.GetCollider()).AddChild(spark);

					spark.GlobalTransform = new Transform(spark.GlobalTransform.basis, bulletRay.GetCollisionPoint());
					spark.Emitting = true;
				}

				// Add Muzzle
				Particles muzzle = (Particles)muzzleScene.Instance();
				barrelNode.AddChild(muzzle);
				muzzle.Emitting = true;

				if (collisionObject is KinematicBody)
				{
					// Add Blood splatter
					Particles splatter = (Particles)splatterScene.Instance();
					((Node)bulletRay.GetCollider()).AddChild(splatter);

					splatter.GlobalTransform = new Transform(splatter.GlobalTransform.basis, bulletRay.GetCollisionPoint());
					splatter.Emitting = true;

					int localDamage = 0;
					if (collisionObject.IsInGroup("head")) { localDamage = (int)GD.RandRange(damage / 2, damage); }
					else { localDamage = (int)GD.RandRange(damage / 3, damage / 2); }

					int colliderId = Convert.ToInt32(collisionObject.Name);

					// Send damage report.
					GameState.instance.EmitSignal(nameof(GameState.takeDamage), colliderId, localDamage);
					return;
				}
				else if (collisionObject.IsInGroup("props") || collisionObject.IsInGroup("walls"))
				{
					// Apply force to rigid body other than hitbox
					if (bulletRay.GetCollider() is RigidBody)
					{
						int localDamage = (int)GD.RandRange(damage / 1.5f, damage);
						((RigidBody)bulletRay.GetCollider()).ApplyCentralImpulse(-bulletRay.GetCollisionNormal() * (localDamage * 0.3f));
					}

					// Apply decal
					Spatial decal = (Spatial)decalScene.Instance();
					((Node)bulletRay.GetCollider()).AddChild(decal);
					decal.GlobalTransform = new Transform(
						decal.GlobalTransform.basis,
						bulletRay.GetCollisionPoint()
					);

					decal.LookAt(bulletRay.GetCollisionPoint() + bulletRay.GetCollisionNormal(), new Vector3(1, 1, 0));
					return;
				}
			}
		}
		else
		{
			AudioStreamPlayer3D empty = ((AudioStreamPlayer3D)audioNode.GetNode("empty"));
			if (!empty.Playing)
			{
				empty.PitchScale = (float)GD.RandRange(0.9, 1.1);
				empty.Play();
			}
		}
	}
	public void reload()
	{

		if (reloadWeapon == false && bullets < maxBullets && ammo > 0)
		{
			reloadWeapon = true;

			// Audio.
			AudioStreamPlayer3D reload = ((AudioStreamPlayer3D)audioNode.GetNode("reload"));
			reload.PitchScale = (float)GD.RandRange(0.9, 1.1);
			reload.Play(.30f);

			animation.Play("reload");

			// Hide crosshair
			crosshair.Visible = false;
		}
	}
	bool reloadWeapon = false;
	private void animationTimeout(String animName)
	{
		if (animName == "reload")
		{
			reloadWeapon = false;

			// Re calculate bullets.
			for (int b = 0; b < ammo; b++)
			{
				bullets += 1;
				ammo -= 1;

				if (bullets >= maxBullets)
				{
					break;
				}
			}

			// Notify ammo update
			GameState.instance.EmitSignal(nameof(GameState.updateWeapon), weaponName, bullets, ammo);

			crosshair.Visible = true;
		}
	}
	public void focus(int input, float delta)
	{
		if (input == 1 && !reloadWeapon)
		{
			if (camera != null) { camera.Fov = Mathf.Lerp(camera.Fov, 50, lerpSpeed * delta); }
			weaponNode.Translation = new Vector3(
				 Mathf.Lerp(-weaponNode.Translation.x, 0f, lerpSpeed * delta),
				 Mathf.Lerp(-weaponNode.Translation.y, -0.6f, lerpSpeed * delta),
				 Mathf.Lerp(-weaponNode.Translation.y, -5f, lerpSpeed * delta)
			 );

			crosshair.RectScale = crosshair.RectScale.LinearInterpolate(new Vector2(.5f, .5f), delta);
		}
		else
		{
			if (camera != null) { camera.Fov = Mathf.Lerp(camera.Fov, 70, lerpSpeed * delta); }
			weaponNode.Translation = new Vector3(
				Mathf.Lerp(-weaponNode.Translation.x, 1.5f, lerpSpeed * delta),
				Mathf.Lerp(-weaponNode.Translation.y, -1.5f, lerpSpeed * delta),
				Mathf.Lerp(-weaponNode.Translation.y, -4, lerpSpeed * delta)
			);
		}
	}
	public void update(float delta)
	{
		if (camera != null)
		{
			camera.Rotation = new Vector3(
				Mathf.Lerp(camera.Rotation.x, 0, 5 * delta),
				Mathf.Lerp(camera.Rotation.y, 0, 5 * delta),
				camera.Rotation.z
			);
		}

		weaponNode.Rotation = new Vector3(
			Mathf.Lerp(weaponNode.Rotation.x, 0, 5 * delta),
			Mathf.Lerp(weaponNode.Rotation.y, 0, 5 * delta),
			Mathf.Lerp(weaponNode.Rotation.z, 0, 5 * delta)
		);

		// Reset crosshair scale
		crosshair.RectScale = crosshair.RectScale.LinearInterpolate(new Vector2(1, 1), 5 * delta);
	}
}
