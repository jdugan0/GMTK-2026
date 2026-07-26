using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Godot;

public partial class GameManager : Node
{
    public static GameManager instance;

    [Export]
    float combatExitDelay = 5f;

    [Export]
    float deathTime;

    public bool InCombat { get; private set; } = false;
    bool reported = false;
    bool dying = false;
    public bool outroTriggered = false;
    float combatExitTimer = 0f;
    float time = 0;
    float randomSoundTimer;

    [Export]
    UI ui;

    [ExportGroup("Outro")]
    [Export]
    float outroFadeOut = 6f;

    [Export]
    float outroGap = 2f;

    [Export]
    float outroFadeIn = 2f;

    [ExportGroup("Leaves")]
    [Export]
    Texture2D[] leafTextures = [];

    [Export]
    int maxLeaves = 40;

    [Export]
    Vector2 leafSpawnInterval = new(0.35f, 1.1f);

    [Export]
    Vector2 leafSpeed = new(80f, 200f);

    [Export]
    Vector2 leafScale = new(0.5f, 1.1f);

    [Export]
    float leafWindAngle = 25f;

    [Export]
    Vector2 leafSwayAmplitude = new(12f, 45f);

    [Export]
    Vector2 leafSwayFrequency = new(0.3f, 1.0f);

    [Export]
    Vector2 leafSpin = new(-1.5f, 1.5f);

    [Export]
    float leafAlpha = 0.7f;

    [Export(PropertyHint.Layers2DRender)]
    int leafLightMask = 8;

    [Export]
    int leafZIndex = 1;

    [ExportGroup("Outro")]
    [Export]
    Texture2D dieSprite1;

    [Export]
    Texture2D dieSprite2;

    class Leaf
    {
        public Sprite2D sprite;
        public Vector2 position;
        public float speed;
        public float swayAmplitude;
        public float swayFrequency;
        public float swayPhase;
        public float spin;
    }

    readonly List<Leaf> leaves = new();
    Node2D leafRoot;
    float leafSpawnTimer;
    float leafTime;

    public override void _Ready()
    {
        instance = this;
        if (LevelManager.instance.currLevel != 6)
        {
            MusicManager.instance.PlaySong("outOfCombatBackground");
        }
        else
        {
            AudioManager.instance.PlaySFXThen("outroIntro", "outroMiddle");
        }
        randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
        SetupLeaves();
    }

    public async void TriggerOutro(Node2D body)
    {
        if (outroTriggered || body is not Movement)
            return;
        Movement m = ((Movement)body);
        ui.heartDisplay.Visible = false;
        ui.countDownLabel.Visible = false;
        outroTriggered = true;
        m.outroStarted = true;
        var tw = GetTree().CreateTween();
        tw.TweenProperty(m.camera, "zoom", new Vector2(1, 1), 10);
        await AudioManager.instance.FadeInto(
            "outroMiddle",
            outroFadeOut,
            "outroEnding",
            outroGap,
            outroFadeIn
        );
        m.outro = true;
        m.sprite2D.Play("OUTRO");
        AudioManager.instance.PlaySFX("heartExplode");
        await ToSignal(m.sprite2D, AnimatedSprite2D.SignalName.AnimationFinished);
        await SceneSwitcher.instance.SwitchSceneAsyncSlide("outro", 1f);
    }

    void SetupLeaves()
    {
        if (leafTextures.Length == 0)
            return;

        leafRoot = new Node2D { Name = "Leaves", ZIndex = leafZIndex };
        AddChild(leafRoot);
        leafSpawnTimer = RandRange(leafSpawnInterval);

        float span = LeafHalfSpan();
        for (int i = 0; i < maxLeaves / 3; i++)
        {
            Leaf leaf = SpawnLeaf();
            leaf.position += WindDirection() * (float)GD.RandRange(0.0, span * 2.0);
        }
    }

    Vector2 WindDirection() => Vector2.Right.Rotated(Mathf.DegToRad(leafWindAngle));

    Vector2 LeafCenter() => GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? Vector2.Zero;

    float LeafHalfSpan()
    {
        Camera2D camera = GetViewport().GetCamera2D();
        Vector2 zoom = camera?.Zoom ?? Vector2.One;
        Vector2 extent = GetViewport().GetVisibleRect().Size / zoom;
        return extent.Length() * 0.5f + 128f;
    }

    Leaf SpawnLeaf()
    {
        Vector2 dir = WindDirection();
        float halfSpan = LeafHalfSpan();
        int r = GD.RandRange(0, leafTextures.Length - 1);
        Leaf leaf = new()
        {
            sprite = new Sprite2D
            {
                Texture = leafTextures[r],
                Modulate = new Color(1f, 1f, 1f, leafAlpha),
                Scale = ((r == 2 ? 6 : 1) * Vector2.One) * RandRange(leafScale),
                Rotation = (float)GD.RandRange(0.0, Mathf.Tau),
                LightMask = leafLightMask,
            },
            speed = RandRange(leafSpeed),
            swayAmplitude = RandRange(leafSwayAmplitude),
            swayFrequency = RandRange(leafSwayFrequency),
            swayPhase = (float)GD.RandRange(0.0, Mathf.Tau),
            spin = RandRange(leafSpin),
        };
        leaf.position =
            LeafCenter()
            - dir * halfSpan
            + dir.Orthogonal() * (float)GD.RandRange(-halfSpan, halfSpan);

        leafRoot.AddChild(leaf.sprite);
        leaves.Add(leaf);
        return leaf;
    }

    public override void _Process(double delta)
    {
        if (leafRoot == null)
            return;

        leafTime += (float)delta;
        leafSpawnTimer -= (float)delta;
        if (leafSpawnTimer <= 0f)
        {
            leafSpawnTimer = RandRange(leafSpawnInterval);
            if (leaves.Count < maxLeaves)
                SpawnLeaf();
        }

        Vector2 dir = WindDirection();
        Vector2 perp = dir.Orthogonal();
        Vector2 center = LeafCenter();
        float halfSpan = LeafHalfSpan();

        for (int i = leaves.Count - 1; i >= 0; i--)
        {
            Leaf leaf = leaves[i];
            leaf.position += dir * leaf.speed * (float)delta;

            Vector2 rel = leaf.position - center;
            if (rel.Dot(dir) > halfSpan || rel.Length() > halfSpan * 2.5f)
            {
                leaf.sprite.QueueFree();
                leaves.RemoveAt(i);
                continue;
            }

            float sway =
                leaf.swayAmplitude
                * Mathf.Sin(leafTime * leaf.swayFrequency * Mathf.Tau + leaf.swayPhase);
            leaf.sprite.Position = leaf.position + perp * sway;
            leaf.sprite.Rotation += leaf.spin * (float)delta;
        }
    }

    static float RandRange(Vector2 range) => (float)GD.RandRange(range.X, range.Y);

    public async void Die(Movement player)
    {
        if (dying || outroTriggered)
            return;
        dying = true;
        ui.Reset();
        player.Reset();
        AudioManager.instance.CancelAllSFX();
        AudioManager.instance.PlaySFX("playerDies");
        foreach (var item in GetTree().GetNodesInGroup("bullet"))
        {
            ((Node2D)item).Visible = false;
        }
        await SceneSwitcher.instance.WaitOneFrame();
        player.sprite2D.Play("DEATH");
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        ui.HeartAnim();
        GetTree().Paused = true;
        await ToSignal(GetTree().CreateTimer(2f), SceneTreeTimer.SignalName.Timeout);
        await SceneSwitcher.instance.SwitchSceneAsyncSlide(
            LevelManager.instance.GetCurrLevel(),
            1f
        );
    }

    public void Win(Movement player)
    {
        AudioManager.instance.CancelAllSFX(
            ["outOfCombatBackground", "outOfCombatRandom"],
            ["combat"]
        );
        // MusicManager.instance.PlaySong("levelWin");
        LevelManager.instance.UnlockLevel(LevelManager.instance.currLevel + 1);
        ui.ShowWin();
        player.arrow.Visible = false;
    }

    public void ReportCombat()
    {
        reported = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (dying)
            return;

        bool rawInCombat = reported;
        reported = false;

        if (!InCombat)
        {
            randomSoundTimer -= (float)delta;
        }
        if (randomSoundTimer <= 0)
        {
            randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
            if (LevelManager.instance.currLevel != 6)
                AudioManager.instance.PlaySFX("outOfCombatRandom");
        }

        if (rawInCombat)
        {
            combatExitTimer = 0f;
            if (!InCombat)
            {
                InCombat = true;
                GD.Print("IN COMBAT");
                time = MusicManager.instance.SongPosition();
                MusicManager.instance.CancelSong();
                MusicManager.instance.PlaySong("combat");
            }
        }
        else if (InCombat)
        {
            combatExitTimer += (float)delta;
            if (combatExitTimer >= combatExitDelay)
            {
                InCombat = false;
                combatExitTimer = 0f;
                randomSoundTimer = (float)GD.RandRange(5.0, 8.0);
                GD.Print("OUT COMBAT");
                const float fade = 4.0f;
                MusicManager.instance.CancelSong(fade);
                float resume = time;
                GetTree().CreateTimer(fade).Timeout += () =>
                {
                    if (!InCombat)
                        MusicManager.instance.PlaySong("outOfCombatBackground", resume);
                };
            }
        }
    }
}
