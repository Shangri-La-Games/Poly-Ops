using Godot;
using System;

public class Lobby : Control
{
	Panel connectPanel;
	Panel playersPanel;
	ItemList playersList;
	LineEdit nameEdit;
	LineEdit ipEdit;
	Button hostButton;
	Button joinButton;
	Button startButton;
	AcceptDialog dialog;

	public override void _Ready()
	{
		// Initialize global events
		GameState.instance.Connect("connectionFailed", this, "connectionFailed");
		GameState.instance.Connect("connectionSuccessful", this, "connectionSuccessful");
		GameState.instance.Connect("playerUpdated", this, "refreshLobby");
		GameState.instance.Connect("gameEnded", this, "gameEnded");
		GameState.instance.Connect("gameError", this, "gameError");

		// Initialize UI events
		connectPanel = (Panel)GetNode("connect");
		playersPanel = (Panel)GetNode("players");
		dialog = (AcceptDialog)GetNode("dialog");

		// Initialize visibility
		connectPanel.Hide();
		playersPanel.Show();

		// -- Name, IP Input --
		nameEdit = (LineEdit)playersPanel.GetNode("v_container/edit_name");
		ipEdit = (LineEdit)playersPanel.GetNode("v_container/edit_ip");

		// Players list
		playersList = (ItemList)connectPanel.GetNode("v_container/list_player");

		// -- Host --
		hostButton = (Button)playersPanel.GetNode("v_container/h_container/btn_host");
		hostButton.Connect("pressed", this, "handleHost");

		// -- Join --
		joinButton = (Button)playersPanel.GetNode("v_container/h_container/btn_join");
		joinButton.Connect("pressed", this, "handleJoin");

		// -- Start --
		startButton = (Button)connectPanel.GetNode("v_container/btn_start");
		startButton.Connect("pressed", this, "handleStart");
	}

	private void handleJoin()
	{
		String name = nameEdit.Text;
		if (name == "")
		{
			dialog.DialogText = "Name is invalid";
			return;
		}

		String ipAddress = ipEdit.Text;
		if (!ipAddress.IsValidIPAddress())
		{
			dialog.DialogText = "Invalid IP address";
			return;
		}

		hostButton.Disabled = true;
		joinButton.Disabled = true;
		dialog.DialogText = "";

		// Join game
		GameState.Instance.joinGame(ipAddress, name);
		refreshLobby();

	}
	private void handleHost()
	{
		String name = nameEdit.Text;
		if (name == "")
		{
			dialog.DialogText = "Name is invalid";
			return;
		}

		// Clear UI
		connectPanel.Show();
		playersPanel.Hide();
		dialog.DialogText = "";

		// Host game
		GameState.Instance.hostGame(nameEdit.Text);
		refreshLobby();
	}
	private void handleStart() { GameState.Instance.startGame(); }
	private void connectionSuccessful()
	{
		connectPanel.Show();
		playersPanel.Hide();
	}
	private void connectionFailed()
	{
		hostButton.Disabled = false;
		joinButton.Disabled = false;
		dialog.DialogText = "Connection failed";
	}
	private void gameEnded()
	{
		// Hide();
		connectPanel.Hide();
		playersPanel.Show();

		hostButton.Disabled = false;
		joinButton.Disabled = false;
	}
	private void gameError(String error)
	{
		dialog.DialogText = error;
	}
	private void refreshLobby()
	{
		playersList.Clear();

		// Add current player.
		playersList.AddItem(String.Format("{0} (You)", GameState.Instance.getPlayerName()));

		// Add other players
		foreach (String p in GameState.Instance.getPlayerList()) { playersList.AddItem(p); }

		// Enable/Disable start button
		startButton.Disabled = !GetTree().IsNetworkServer();
	}

}
