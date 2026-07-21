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
        player.Finished += () => playing.Remove(id);
        player.Finished += () => playingByName.Remove(sound);
        player.Finished += () => names.Remove(id);
        player.Finished += () => player.QueueFree();
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

    public (AudioStreamPlayer, Guid) PlaySFX(string sound)
    {
        return PlaySFX(this, sound, 0);
    }

    public (AudioStreamPlayer, Guid) PlaySFX(string sound, float time)
    {
        return PlaySFX(this, sound, time);
    }

    public (bool, AudioStreamPlayer) CancelSFX(Guid id)
    {
        if (IsPlaying(id))
        {
            var p = playing[id];
            playing.Remove(id);
            playingByName.Remove(names[id]);
            names.Remove(id);
            p.QueueFree();
            return (true, p);
        }
        return (false, null);
    }

    public (bool, AudioStreamPlayer) CancelSFX(string sound)
    {
        foreach (var s in GetPlaying(sound))
        {
            playingByName.Remove(sound);
            playing.Remove(s.id);
            names.Remove(s.id);
            s.p.QueueFree();
            return (true, s.p);
        }
        return (false, null);
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
}
