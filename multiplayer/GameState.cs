using Godot;
using System;
using System.Collections.Generic;

public class GameState : Node
{
    public static GameState instance;

    const int PORT = 2345;
    const int MAX_PLAYERS = 8;
    private String playerName;
    [Signal] public delegate void mouseCaptured();
    [Signal] public delegate void takeDamage(int peerId, int damage);
    [Signal] public delegate void connectionSuccessful();
    [Signal] public delegate void connectionFailed();
    [Signal] public delegate void playerUpdated();
    [Signal] public delegate void gameEnded();
    [Signal] public delegate void gameError(String error);
    public Dictionary<int, String> players = new Dictionary<int, String>();

    public static GameState Instance { get { return instance; } }

    GameState() { instance = this; }

    public Node GetMainNode()
    {
        Node root = GetTree().Root;
        return root.GetChild(root.GetChildCount() - 1);
    }

    public override void _Ready()
    {
        generateName();

        // Initialize listeners.
        GetTree().Connect("network_peer_connected", this, "peerConnected");
        GetTree().Connect("network_peer_disconnected", this, "peerDisconnected");
        GetTree().Connect("connected_to_server", this, "serverConnected");
        GetTree().Connect("connection_failed", this, "serverConnectionFailed");
        GetTree().Connect("server_disconnected", this, "serverDisconnected");
    }
    public void generateName() { playerName = "Hello"; }
    public List<String> getPlayerList() { return new List<String>(players.Values); }
    public String getPlayerName() { return playerName; }
    public void hostGame(String newName)
    {
        // Assign new name.
        playerName = newName;

        // Create server
        NetworkedMultiplayerENet host = new NetworkedMultiplayerENet();
        host.CreateServer(PORT, MAX_PLAYERS);
        GetTree().NetworkPeer = host;
    }
    public void joinGame(String IPAddress, String newName)
    {
        // Assign new data
        playerName = newName;

        // Create client
        NetworkedMultiplayerENet host = new NetworkedMultiplayerENet();
        host.CreateClient(IPAddress, PORT);
        GetTree().NetworkPeer = host;
    }
    private void peerConnected(int networkId) { }
    private void peerDisconnected(int networkId)
    {
        if (GetTree().IsNetworkServer())
        {
            if (HasNode("/root/Main"))
            {
                EmitSignal("gameError", String.Format("Player {0} disconnected", players[networkId]));
                endGame();
            }
            else
            {
                unRegisterPlayer(networkId);
                foreach (int playerId in players.Keys) { RpcId(playerId, "unRegisterPlayer", playerId); }
            }
        }
    }
    private void serverConnected()
    {
        Rpc("registerPlayer", GetTree().GetNetworkUniqueId(), playerName);
        EmitSignal("connectionSuccessful");
    }
    private void serverDisconnected()
    {
        EmitSignal("gameError", "Server disconnected");
        endGame();
    }

    private void serverConnectionFailed()
    {
        GetTree().NetworkPeer = null;
        EmitSignal("connectionFailed");
    }

    [Remote]
    public void preStart(Dictionary<int, int> spawnPoints)
    {
        // Change scene
        PackedScene arenaScene = (PackedScene)ResourceLoader.Load("res://multiplayer/arena/arena.tscn");
        Arena arena = (Arena)arenaScene.Instance();

        // Add arena
        GetTree().Root.AddChild(arena);

        // Hide lobby
        ((Control)GetTree().Root.GetNode("lobby")).Hide();

        // Add players
        arena.addPlayers(spawnPoints);

        if (!GetTree().IsNetworkServer())
        {
            RpcId(1, "readyToStart", GetTree().GetNetworkUniqueId());
        }
        else if (players.Count == 0)
        {
            postStart();
        }
    }
    public void startGame()
    {
        if (!GetTree().IsNetworkServer()) { throw new Exception("Not a server"); }

        // Spawn location
        Dictionary<int, int> spawnPoints = new Dictionary<int, int>();
        spawnPoints[1] = 0;

        int spawnIndex = 1;

        // Assign spawn points to each players.
        foreach (KeyValuePair<int, String> player in players)
        {
            spawnPoints[player.Key] = spawnIndex;
            spawnIndex++;
        }

        // Spawn players for in players
        foreach (KeyValuePair<int, String> player in players)
        {
            RpcId(player.Key, "preStart", spawnPoints);
        }

        // Prepare game
        preStart(spawnPoints);
    }

    [Remote]
    public void postStart() { GetTree().Paused = false; }
    public void quitGame() { endGame(); GetTree().Quit(); }

    public void endGame()
    {
        if (HasNode("/root/Main")) { GetNode("/root/Main").QueueFree(); }

        EmitSignal("gameEnded");
        players.Clear();
        GetTree().NetworkPeer = null;
    }

    List<int> readyPlayers = new List<int>();

    [Remote]
    public void readyToStart(int networkId)
    {
        // Add player
        if (!readyPlayers.Contains(networkId)) { readyPlayers.Add(networkId); }

        // Start
        if (readyPlayers.Count == players.Count)
        {
            foreach (KeyValuePair<int, String> player in players)
            {
                RpcId(player.Key, "postStart");
            }
            postStart();
        }
    }


    [Remote]
    public void registerPlayer(int networkId, String name)
    {
        if (GetTree().IsNetworkServer())
        {
            // If server, notify all the players.
            RpcId(networkId, "registerPlayer", 1, playerName);
            foreach (KeyValuePair<int, String> player in players)
            {
                RpcId(networkId, "registerPlayer", player.Key, player.Value);
                RpcId(networkId, "registerPlayer", networkId, name);
            }
        }

        players[networkId] = name;
        EmitSignal("playerUpdated");
    }

    [Remote]
    public void unRegisterPlayer(int networkId)
    {
        players.Remove(networkId);
        EmitSignal("playerUpdated");
    }
}
