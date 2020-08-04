using Godot;
using System;
using System.Collections.Generic;

public class Player : KinematicBody
{

    [Export]
    NodePath headPath;
    public CollisionShape headNode;
    RayCast headRay;

    // Speed variables
    public float normalSpeed = 04;
    public float sprintSpeed = 12;
    public float walkSpeed = 8;
    public float crouchSpeed = 10;

    // Physics variables
    public float gravity = 50;
    public float jumpHeight = 15;
    public float friction = 25;
    public int health;
    public Vector3 velocity = new Vector3();
    public Vector3 direction = new Vector3();
    public Vector3 acceleration = new Vector3();
    public Dictionary<String, int> input = new Dictionary<String, int>();
    public bool isPuppetController() { return getPuppetController() is PuppetController; }
    public Node getPuppetController() { return ((Node)GetNode("head").GetChild(1)); }
    public void intializeGUI()
    {
        Label userLabel = (Label)GetNode("HUD/stats/label_user");
        userLabel.Text = GetTree().GetNetworkUniqueId().ToString();
    }

    public void pause() { ((HUD)GetNode("HUD")).showPause(); }
    public void resume() { ((HUD)GetNode("HUD")).hidePause(); }
    public override void _Ready()
    {
        headNode = (CollisionShape)GetNode(headPath);
        headRay = (RayCast)headNode.GetNode("head");

        initSignals();
        initPlayer();
        intializeGUI();
        initInput();
        changeBodyMaterial();
    }

    private void initPlayer()
    {
        health = 100;
    }

    private void initSignals()
    {
        GameState.instance.Connect(nameof(GameState.takeDamage), this, nameof(updateDamage));
        GameState.instance.Connect(nameof(GameState.updateWeapon), this, nameof(updateWeaponStat));

        if (isPuppetController())
        {
            GameState.instance.Connect(nameof(GameState.mouseCaptured), getPuppetController(), nameof(PuppetController.captureMouse));
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        // Handle movement
        movement(delta);

        // Handle couch
        crouch(delta);

        // Handle jump
        jump(delta);

        // Handle sprint
        sprint(delta);

        // Handle death;
        death(delta);

        if (isPuppetController()) { RpcUnreliable("networkUpdate", input, Translation, Rotation, headNode.Rotation); }
    }

    private void initInput()
    {
        input["left"] = 0;
        input["right"] = 0;
        input["forward"] = 0;
        input["back"] = 0;
        input["switch"] = 0;
        input["shoot"] = 0;
        input["reload"] = 0;
        input["focus"] = 0;
        input["crouch"] = 0;
        input["jump"] = 0;
        input["sprint"] = 0;
    }

    // Handle player movement
    private void movement(float delta)
    {
        // Apply gravity
        if (this.IsOnFloor())
        {
            direction = new Vector3();
        }
        else
        {
            direction = direction.LinearInterpolate(new Vector3(), friction * delta);
            velocity.y += -gravity * delta;
        }

        // Apply player direction
        Basis transformbasis = headRay.GlobalTransform.basis;

        direction += (-input["left"] + input["right"]) * transformbasis.x;
        direction += (-input["forward"] + input["back"]) * transformbasis.z;

        direction.y = 0;
        direction = direction.Normalized();

        // Interpolate between current and future positions
        Vector3 target = direction * normalSpeed;
        direction.y = 0;

        Vector3 tempVelocity = velocity.LinearInterpolate(target, normalSpeed * delta);

        // Apply interpolation
        velocity.x = tempVelocity.x;
        velocity.z = tempVelocity.z;

        // Move character
        velocity = MoveAndSlide(velocity, new Vector3(0, 1, 0), false, 6, (float)(Math.PI / 4.0), false);
    }

    // Handle player crouch
    private void crouch(float delta)
    {
        if (!headRay.IsColliding())
        {
            CollisionShape shape = (CollisionShape)GetNode("body");
            CylinderShape capsule = ((CylinderShape)shape.Shape);

            // Height of collision shape
            float newCollisionHeight = Mathf.Lerp(capsule.Height, 2 - (input["crouch"] * 1.5f), crouchSpeed * delta);
            capsule.Height = newCollisionHeight;
        }
    }

    private bool wasJumping = false;
    // Handle player jump
    private void jump(float delta)
    {
        if (input["jump"] == 1)
        {
            if (this.IsOnFloor())
            {
                velocity.y = jumpHeight; wasJumping = true;

                AudioStreamPlayer3D jump = ((AudioStreamPlayer3D)GetNode("audio/jump/0"));
                if (!jump.Playing)
                {
                    jump.PitchScale = (float)GD.RandRange(0.9, 1.1);
                    jump.Play(0);
                }
            }
        }
    }

    // Handle player sprint
    private void sprint(float delta)
    {
        if (input["crouch"] == 0)
        {
            float toggleSpeed = walkSpeed + ((sprintSpeed - walkSpeed) * input["sprint"]);
            normalSpeed = Mathf.Lerp(normalSpeed, toggleSpeed, 3 * delta);
        }
        else { normalSpeed = Mathf.Lerp(normalSpeed, walkSpeed, delta); }
    }

    private void death(float delta)
    {
        if (health <= 0)
        {
            AudioStreamPlayer3D die = ((AudioStreamPlayer3D)GetNode("audio/die/0"));
            if (!die.Playing)
            {
                die.PitchScale = (float)GD.RandRange(0.9, 1.1);
                die.Play();
            }
        }
    }

    public Camera getCamera()
    {
        CharacterController characterController = (CharacterController)GetNode(headPath).GetChild(1);

        if (characterController is PuppetController) { return (Camera)characterController.GetNode("camera"); }
        else { return null; }
    }

    [Sync]
    private void networkUpdate(Dictionary<String, int> input, Vector3 translation, Vector3 rotation, Vector3 headRotation)
    {
        this.input = input;
        this.Translation = translation;
        this.Rotation = rotation;
        this.headNode.Rotation = headRotation;
    }

    private void updateDamage(int id, int damage) { RpcId(id, "inflictDamage", id, damage); }
    private void updateWeapon(String weaponName, int bullets, int ammo) { Rpc("updateWeaponStat", weaponName, bullets, ammo); }

    [Sync]
    private void inflictDamage(int playerId, int damage)
    {
        this.health -= damage;

        // Notify death
        if (this.health < 0)
        {
            GetTree().ReloadCurrentScene();
        }

        // Update health UI
        Label healthLabel = (Label)GetNode("HUD/stats/health/container/label_health");
        healthLabel.Text = this.health.ToString();
    }

    [Sync]
    private void updateWeaponStat(String weaponName, int bullets, int ammo)
    {
        Label weaponLabel = (Label)GetNode("HUD/stats/weapon/v_container/label_name");
        weaponLabel.Text = weaponName;

        Label bulletsLabel = (Label)GetNode("HUD/stats/weapon/v_container/h_container/label_bullets");
        bulletsLabel.Text = bullets.ToString();

        Label ammoLabel = (Label)GetNode("HUD/stats/weapon/v_container/h_container/label_ammo");
        ammoLabel.Text = ammo.ToString();
    }

    SpatialMaterial enemyMaterialScene = (SpatialMaterial)ResourceLoader.Load("res://multiplayer/prefabs/player/material/colorEnemy.tres");
    SpatialMaterial partnerMaterialScene = (SpatialMaterial)ResourceLoader.Load("res://multiplayer/prefabs/player/material/colorPartner.tres");

    private void changeBodyMaterial()
    {
        SpatialMaterial newMaterial;

        if (IsNetworkMaster()) { newMaterial = partnerMaterialScene; }
        else { newMaterial = enemyMaterialScene; }

        // Set material
        ((MeshInstance)GetNode("body/mesh")).SetSurfaceMaterial(0, newMaterial);
    }


}
