using Godot;
using System;

// Represents oneself
public class PuppetController : CharacterController
{
    private bool captured = true;
    private float sensibility = 0.2f;
    public void captureMouse() { this.captured = !captured; }

    public override void _PhysicsProcess(float delta) { handleInput(); toggleMouse(); }
    public override void _Input(InputEvent @event) { handleMouse(@event); }

    private void handleInput()
    {
        // Do not allow input if mouse is inactive.
        if (!captured) { return; }

        this.player.input["left"] = Convert.ToInt32(Input.IsActionPressed("KEY_A"));
        this.player.input["right"] = Convert.ToInt32(Input.IsActionPressed("KEY_D"));
        this.player.input["forward"] = Convert.ToInt32(Input.IsActionPressed("KEY_W"));
        this.player.input["back"] = Convert.ToInt32(Input.IsActionPressed("KEY_S"));
        this.player.input["switch"] = Convert.ToInt32(Input.IsActionJustPressed("KEY_E"));
        this.player.input["shoot"] = Convert.ToInt32(Input.IsActionPressed("MOUSE_LEFT"));
        this.player.input["reload"] = Convert.ToInt32(Input.IsActionPressed("KEY_R"));
        this.player.input["focus"] = Convert.ToInt32(Input.IsActionPressed("MOUSE_RIGHT"));
        this.player.input["crouch"] = Convert.ToInt32(Input.IsActionPressed("KEY_CTRL"));
        this.player.input["jump"] = Convert.ToInt32(Input.IsActionPressed("KEY_SPACE"));
        this.player.input["sprint"] = Convert.ToInt32(Input.IsActionPressed("KEY_SHIFT"));
    }

    // Toggle mouse capture
    private void toggleMouse()
    {
        if (Input.IsActionJustPressed("KEY_ESCAPE")) { captured = !captured; }
        if (captured) { Input.SetMouseMode(Input.MouseMode.Captured); this.player.resume(); }
        else { Input.SetMouseMode(Input.MouseMode.Visible); this.player.pause(); }
    }

    private void handleMouse(InputEvent @event)
    {
        if (captured)
        {
            if (@event is InputEventMouseMotion)
            {
                InputEventMouseMotion input = @event as InputEventMouseMotion;

                float newRotationX = player.headNode.Rotation.x + -Mathf.Deg2Rad(input.Relative.y * sensibility);
                player.headNode.Rotation = new Vector3(newRotationX, player.headNode.Rotation.y, player.headNode.Rotation.z);

                float newRotationY = player.Rotation.y + -Mathf.Deg2Rad(input.Relative.x * sensibility);
                player.Rotation = new Vector3(player.Rotation.x, newRotationY, player.Rotation.z);
            }

            // Create a limit for camera on x-axis
            Int32 maxAngle = 60;
            player.headNode.Rotation = new Vector3(Math.Min(player.headNode.Rotation.x, Mathf.Deg2Rad(maxAngle)), player.headNode.Rotation.y, player.headNode.Rotation.z);
            player.headNode.Rotation = new Vector3(Math.Max(player.headNode.Rotation.x, -Mathf.Deg2Rad(maxAngle)), player.headNode.Rotation.y, player.headNode.Rotation.z);
        }
    }

}
