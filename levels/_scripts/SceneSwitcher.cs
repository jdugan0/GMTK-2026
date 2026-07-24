using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class SceneSwitcher : Node
{
    public static SceneSwitcher instance = null;

    [Export]
    public SceneResource[] scenes;
    private Dictionary<string, PackedScene> sceneDict = new();

    [Export]
    private Control fadeRect;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        instance = this;
        foreach (var s in scenes)
        {
            sceneDict.Add(s.sceneName, s.scene);
        }
    }

    public async Task SwitchSceneAsyncSlide(string sceneName)
    {
        await WaitOneFrame();
        GetTree().Paused = true;
        await SlideIn(0.35);
        GetTree().ChangeSceneToPacked(sceneDict[sceneName]);
        await WaitOneFrame();
        await SlideOut(0.35);
        fadeRect.Visible = false;
        GetTree().Paused = false;
    }

    public void SwitchScene(int loadOrder)
    {
        GetTree().ChangeSceneToPacked(scenes[loadOrder].scene);
    }

    private async Task WaitOneFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    public void SwitchScene(string sceneName)
    {
        GetTree().ChangeSceneToPacked(sceneDict[sceneName]);
    }

    private async Task SlideIn(double dur)
    {
        var size = GetViewport().GetVisibleRect().Size;
        fadeRect.Visible = true;
        fadeRect.Position = new Vector2(-size.X, 0);
        var t = GetTree().CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        t.TweenProperty(fadeRect, "position:x", 0, dur)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(t, Tween.SignalName.Finished);
    }

    private async Task SlideOut(double dur)
    {
        var size = GetViewport().GetVisibleRect().Size;
        var t = GetTree().CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        t.TweenProperty(fadeRect, "position:x", size.X, dur)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(t, Tween.SignalName.Finished);
    }
}
