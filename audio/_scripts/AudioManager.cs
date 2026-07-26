using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

public partial class AudioManager : Node
{
    private const string SettingsPath = "user://settings.cfg";

    [Export]
    public Sound[] sounds;
    private Dictionary<string, Sound> dict = new();
    public static AudioManager instance;
    public Dictionary<Guid, AudioStreamPlayer> playing = new();
    public Dictionary<string, List<(AudioStreamPlayer p, Guid id)>> playingByName = new();
    public Dictionary<Guid, string> names = new();

    public override void _Ready()
    {
        if (instance == null)
        {
            instance = this;
            LoadSettings();
        }
        else
        {
            this.QueueFree();
        }
        foreach (Sound s in sounds)
        {
            dict.Add(s.name, s);
        }
    }

    private float masterVolume = 1f;

    /// Linear 0-1, applied to the Master bus.
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp(value, 0f, 1f);
            int bus = AudioServer.GetBusIndex("Master");
            AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(masterVolume));
            AudioServer.SetBusMute(bus, masterVolume <= 0.001f);
        }
    }

    public void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("audio", "master", masterVolume);
        config.Save(SettingsPath);
    }

    private void LoadSettings()
    {
        var config = new ConfigFile();
        MasterVolume =
            config.Load(SettingsPath) == Error.Ok
                ? (float)config.GetValue("audio", "master", 1f)
                : 1f;
    }

    public (AudioStreamPlayer, Guid) PlaySFX(Node from, string sound, float time)
    {
        var player = new AudioStreamPlayer();
        Sound s;
        s = dict[sound];
        player.Stream = s.stream;
        player.VolumeDb = s.volume;
        // playing.Add(s, player);
        Guid id = Guid.NewGuid();
        player.Finished += () =>
        {
            playing.Remove(id);
            RemoveByName(sound, id);
            names.Remove(id);
            player.QueueFree();
        };
        // player.Finished += ()=>playing.Remove(s);
        from.AddChild(player);
        playing.Add(id, player);
        if (!playingByName.ContainsKey(sound))
        {
            playingByName.Add(sound, new());
        }
        playingByName[sound].Add((player, id));
        names.Add(id, sound);
        player.Play(time);
        return (player, id);
    }

    public (AudioStreamPlayer, Guid) PlaySFX(Node from, string sound)
    {
        return PlaySFX(from, sound, 0);
    }

    public (AudioStreamPlayer p, Guid id) PlaySFX(string sound)
    {
        return PlaySFX(this, sound, 0);
    }

    public (AudioStreamPlayer, Guid) PlaySFX(string sound, float time)
    {
        return PlaySFX(this, sound, time);
    }

    public (AudioStreamPlayer p, Guid id, Tween fade) PlaySFXFadeIn(
        Node from,
        string sound,
        float fadeDuration,
        float time
    )
    {
        var (player, id) = PlaySFX(from, sound, time);
        float targetDb = dict[sound].volume;
        player.VolumeDb = -80f;
        Tween tween = player.CreateTween();
        tween.TweenProperty(player, "volume_db", targetDb, fadeDuration);
        return (player, id, tween);
    }

    public (AudioStreamPlayer p, Guid id, Tween fade) PlaySFXFadeIn(
        Node from,
        string sound,
        float fadeDuration
    )
    {
        return PlaySFXFadeIn(from, sound, fadeDuration, 0);
    }

    public (AudioStreamPlayer p, Guid id, Tween fade) PlaySFXFadeIn(
        string sound,
        float fadeDuration
    )
    {
        return PlaySFXFadeIn(this, sound, fadeDuration, 0);
    }

    public (bool cancelled, AudioStreamPlayer p, Tween fade) CancelSFXFadeOut(
        Guid id,
        float fadeDuration
    )
    {
        if (!IsPlaying(id))
        {
            return (false, null, null);
        }
        var p = playing[id];
        Tween tween = p.CreateTween();
        CancelSFXNoFree(id);
        tween.TweenProperty(p, "volume_db", -80f, fadeDuration);
        tween.Finished += () =>
        {
            p.Stop();
            p.QueueFree();
        };
        return (true, p, tween);
    }

    private (bool, AudioStreamPlayer p) CancelSFXNoFree(Guid id)
    {
        if (IsPlaying(id))
        {
            var p = playing[id];
            playing.Remove(id);
            RemoveByName(names[id], id);
            names.Remove(id);
            return (true, p);
        }
        return (false, null);
    }

    public (bool cancelled, AudioStreamPlayer p, Tween fade) CancelSFXFadeOut(
        string sound,
        float fadeDuration
    )
    {
        AudioStreamPlayer last = null;
        Tween lastFade = null;
        foreach (var s in GetPlaying(sound).ToList())
        {
            var (cancelled, p, fade) = CancelSFXFadeOut(s.id, fadeDuration);
            if (cancelled)
            {
                last = p;
                lastFade = fade;
            }
        }
        return (last != null, last, lastFade);
    }

    public async Task<(AudioStreamPlayer p, Guid id)> FadeInto(
        string sound,
        float fadeOut,
        string next,
        float gap = 0f,
        float fadeIn = 0f
    )
    {
        var (cancelled, _, fade) = CancelSFXFadeOut(sound, fadeOut);
        if (cancelled)
            await ToSignal(fade, Tween.SignalName.Finished);

        if (gap > 0f)
            await ToSignal(GetTree().CreateTimer(gap), SceneTreeTimer.SignalName.Timeout);

        if (fadeIn <= 0f)
            return PlaySFX(next);

        var (p, id, rise) = PlaySFXFadeIn(next, fadeIn);
        await ToSignal(rise, Tween.SignalName.Finished);
        return (p, id);
    }

    public void PlaySFXThen(string sound, string next)
    {
        PlaySFX(sound).p.Finished += () => PlaySFX(next);
    }

    public (bool, AudioStreamPlayer) CancelSFX(Guid id)
    {
        if (IsPlaying(id))
        {
            var p = playing[id];
            playing.Remove(id);
            RemoveByName(names[id], id);
            names.Remove(id);
            p.Stop();
            p.QueueFree();
            return (true, p);
        }
        return (false, null);
    }

    public (bool cancel, AudioStreamPlayer p) CancelSFX(string sound)
    {
        AudioStreamPlayer last = null;
        foreach (var s in GetPlaying(sound).ToList())
        {
            playing.Remove(s.id);
            names.Remove(s.id);
            s.p.Stop();
            s.p.QueueFree();
            last = s.p;
        }
        playingByName.Remove(sound);
        return (last != null, last);
    }

    private void RemoveByName(string sound, Guid id)
    {
        if (playingByName.TryGetValue(sound, out var list))
        {
            list.RemoveAll(e => e.id == id);
            if (list.Count == 0)
            {
                playingByName.Remove(sound);
            }
        }
    }

    public List<(AudioStreamPlayer p, Guid id)> GetPlaying(string sound)
    {
        if (playingByName.ContainsKey(sound))
        {
            return playingByName[sound];
        }
        return new List<(AudioStreamPlayer, Guid)>();
    }

    public bool IsPlaying(Guid id)
    {
        return playing.ContainsKey(id);
    }

    public void CancelAllSFX()
    {
        foreach (var s in playing.Keys.ToList())
        {
            CancelSFX(s);
        }
    }

    public void CancelAllSFX(HashSet<string> invalid, HashSet<string> fades)
    {
        foreach (var s in playing.Keys.ToList())
        {
            if (fades.Contains(names[s]))
            {
                CancelSFXFadeOut(s, 3f);
            }
            else if (!invalid.Contains(names[s]))
            {
                CancelSFX(s);
            }
        }
    }
}
