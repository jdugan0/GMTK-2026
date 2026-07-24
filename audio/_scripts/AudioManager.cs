using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;

public partial class AudioManager : Node
{
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

    public (AudioStreamPlayer, Guid) PlaySFXFadeIn(
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
        return (player, id);
    }

    public (AudioStreamPlayer, Guid) PlaySFXFadeIn(Node from, string sound, float fadeDuration)
    {
        return PlaySFXFadeIn(from, sound, fadeDuration, 0);
    }

    public (AudioStreamPlayer p, Guid id) PlaySFXFadeIn(string sound, float fadeDuration)
    {
        return PlaySFXFadeIn(this, sound, fadeDuration, 0);
    }

    public (bool, AudioStreamPlayer) CancelSFXFadeOut(Guid id, float fadeDuration)
    {
        if (!IsPlaying(id))
        {
            return (false, null);
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
        return (true, p);
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

    public (bool, AudioStreamPlayer p) CancelSFXFadeOut(string sound, float fadeDuration)
    {
        AudioStreamPlayer last = null;
        foreach (var s in GetPlaying(sound).ToList())
        {
            var (cancelled, p) = CancelSFXFadeOut(s.id, fadeDuration);
            if (cancelled)
            {
                last = p;
            }
        }
        return (last != null, last);
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
}
