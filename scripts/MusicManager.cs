using System;
using Godot;

public partial class MusicManager : Node
{
    string currentSong;
    public static MusicManager instance;

    public override void _Ready()
    {
        instance = this;
    }

    public AudioStreamPlayer PlaySong(string song, float time)
    {
        if (song == currentSong)
            return null;
        CancelSong();
        AudioManager.instance.CancelSFX(song);
        currentSong = song;
        return AudioManager.instance.PlaySFX(song, time).Item1;
    }

    public void PlaySong(string song)
    {
        PlaySong(song, 0f);
    }

    public float SongPosition()
    {
        if (currentSong == null)
            return 0f;
        var players = AudioManager.instance.GetPlaying(currentSong);
        return players.Count > 0 ? players[0].p.GetPlaybackPosition() : 0f;
    }

    public (bool, AudioStreamPlayer p) CancelSong()
    {
        if (currentSong == null)
            return (false, null);
        var result = AudioManager.instance.CancelSFX(currentSong);
        currentSong = null;
        return result;
    }

    public (bool cancelled, AudioStreamPlayer p, Tween fade) CancelSong(float dur)
    {
        if (currentSong == null)
            return (false, null, null);
        var result = AudioManager.instance.CancelSFXFadeOut(currentSong, dur);
        currentSong = null;
        return result;
    }
}
