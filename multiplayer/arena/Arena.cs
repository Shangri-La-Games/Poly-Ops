using Godot;
using System;
using System.Collections.Generic;

public class Arena : Spatial
{

    PackedScene opponentScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/opponent/opponent.tscn");
    PackedScene puppetScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/puppet/puppet.tscn");
    PackedScene characterScene = (PackedScene)ResourceLoader.Load("res://multiplayer/prefabs/player/player.tscn");

    public void addPlayers(Dictionary<int, int> spawnPoints)
    {
        foreach (KeyValuePair<int, int> point in spawnPoints)
        {
            createPlayer(point.Key, point.Value, point.Key != GetTree().GetNetworkUniqueId());
        }
    }

    public void removePlayer(int networkId)
    {
        GetNode("players").RemoveChild(GetNode("players").GetNode(networkId.ToString()));
    }

    private void createPlayer(int networkId, int index, Boolean isOpponent)
    {

        // Initialize player / peer.
        CharacterController controller;
        if (isOpponent)
        {
            controller = (OpponentController)opponentScene.Instance();
        }
        else
        {
            controller = (PuppetController)puppetScene.Instance();
        }

        // Initialize character
        Player character = (Player)characterScene.Instance();

        character.GetNode("head").AddChild(controller);
        controller.Name = "controller";
        character.Name = networkId.ToString();

        // Move character to location.
        character.GlobalTransform = new Transform(
            character.Transform.basis,
            ((Position3D)GetNode("spawn").GetNode(String.Format("{0}", index))).Transform.origin
        );

        // Add character to scene
        GetNode("players").AddChild(character);

        // Enable camera.
        ((Camera)controller.GetNode("camera")).Current = !isOpponent;
    }

}
