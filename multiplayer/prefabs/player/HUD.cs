using Godot;
using System;

public class HUD : CanvasLayer
{
    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;

        Button resumeButton = (Button)GetNode("pause/panel/v_container/btn_resume");
        resumeButton.Connect("pressed", this, "resumeGame");

        Button quitButton = (Button)GetNode("pause/panel/v_container/btn_quit");
        quitButton.Connect("pressed", this, "quitGame");
    }
    public void showPause()
    {
        ((Control)GetNode("pause")).Show();
        ((Control)GetNode("crosshair")).Hide();
        ((Control)GetNode("stats")).Hide();
    }
    public void hidePause()
    {
        ((Control)GetNode("pause")).Hide();
        ((Control)GetNode("crosshair")).Show();
        ((Control)GetNode("stats")).Show();
    }

    private void resumeGame() { GameState.instance.EmitSignal("mouseCaptured"); }
    private void quitGame() { GameState.Instance.quitGame(); }

}
