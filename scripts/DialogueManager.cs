using System;
using Godot;

public partial class DialogueManager : TextureRect
{
    public static DialogueManager instance;

    [Export]
    TextureRect disp;

    [Export]
    Label text;

    [Export]
    Texture2D[] sprites;

    [Export]
    Line[] lines;

    [Export]
    Area2D[] triggers;

    [Export]
    float charsPerSecond = 30f;

    [Export]
    float holdDuration = 2f;

    [Export]
    float fadeDuration = 0.5f;

    float revealed;
    int shownChars;
    bool revealing;
    float baseAlpha;
    Tween fade;

    public override void _Ready()
    {
        instance = this;
        baseAlpha = disp.Modulate.A;
        for (int i = 0; i < lines.Length; i++)
        {
            Line line = lines[i];
            Area2D trigger = triggers[i];
            triggers[i].BodyEntered += (Node2D body) => TriggerLine(body, line, trigger);
        }
    }

    public override void _Process(double delta)
    {
        if (!revealing)
            return;

        revealed += (float)delta * charsPerSecond;
        if (revealed >= text.Text.Length)
        {
            revealed = text.Text.Length;
            revealing = false;
            StartFade();
        }

        int shown = (int)revealed;
        if (shown > shownChars)
        {
            shownChars = shown;
            OnCharacterRevealed(text.Text[shown - 1]);
        }
        text.VisibleCharacters = shown;
    }

    void OnCharacterRevealed(char c)
    {
        AudioManager.instance.PlaySFX("textBox");
    }

    void StartFade()
    {
        fade = CreateTween();
        fade.TweenInterval(holdDuration);
        fade.TweenProperty(disp, "modulate:a", 0f, fadeDuration);
        fade.TweenCallback(Callable.From(() => disp.Visible = false));
    }

    public void TriggerLine(Node2D body, Line line, Area2D t)
    {
        if (!(body is Movement))
            return;

        fade?.Kill();
        fade = null;
        t.QueueFree();

        Color modulate = disp.Modulate;
        modulate.A = baseAlpha;
        disp.Modulate = modulate;

        disp.Visible = true;
        disp.Texture = sprites[line.emotion];

        text.Text = line.line;
        text.VisibleCharacters = 0;
        revealed = 0f;
        shownChars = 0;
        revealing = true;
    }
}
