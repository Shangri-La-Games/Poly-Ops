using Godot;
using System;

// Get referene to the character.
// AI or player inherits from this class
public class CharacterController : Spatial
{
	public Player player;

	public override void _Ready()
	{
		// Player > Head > CharacterController
		this.player = (Player)GetParent().GetParent();
	}
}
