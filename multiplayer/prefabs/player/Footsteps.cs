using Godot;
using System;

public class Footsteps : Spatial
{

	[Export]
	NodePath feetNode;

	[Export]
	NodePath playerNode;

	private KinematicBody player;
	private RayCast feet;
	private float footStepTimer = 0f;
	private Godot.Collections.Dictionary<String, Node> footStepList = new Godot.Collections.Dictionary<string, Node>();

	public override void _Ready()
	{
		GD.Randomize();

		player = (KinematicBody)GetNode(playerNode);
		feet = (RayCast)GetNode(feetNode);

		for (int i = 0; i < GetChildCount(); i++)
		{
			footStepList[GetChild(i).Name] = (Node)GetChild(i);
		}
	}

	public override void _Process(float delta)
	{
		if (footStepTimer <= 0)
		{
			if ((Vector3)player.Get("direction") != Vector3.Zero && feet.IsColliding())
			{
				// Get collided body group name
				Godot.Collections.Array collidedGroups = ((Node)feet.GetCollider()).GetGroups();

				foreach (String group in collidedGroups)
				{
					if (footStepList.ContainsKey(group))
					{
						Node footStepNode = footStepList[group];
						if (footStepNode.GetChildCount() > 0)
						{

							// Play audio
							int randomIndex = (int)GD.RandRange(0, footStepNode.GetChildCount() - 1);
							AudioStreamPlayer3D audio = (AudioStreamPlayer3D)footStepNode.GetChild(randomIndex);
							audio.Play();

							// Sync player speed with audio loop
							footStepTimer = (1 - (0.06f * (float)player.Get("normalSpeed")));
							break;
						}
					}
				}
			}
		}
		else
		{
			footStepTimer -= delta;
		}
	}
}
