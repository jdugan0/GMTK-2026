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

    public void PlaySong(string song, float time)
    {
        if (song == currentSong)
            return;
        if (currentSong != null)
        {
            CancelSong();
        }
        currentSong = song;
        AudioManager.instance.PlaySFX(song, time);
    }

    public void PlaySong(string song)
    {
        PlaySong(song, 0f);
    }

    public (bool, AudioStreamPlayer p) CancelSong()
    {
        return AudioManager.instance.CancelSFX(currentSong);
    }

    public (bool, AudioStreamPlayer p) CancelSong(float dur)
    {
        return AudioManager.instance.CancelSFXFadeOut(currentSong, dur);
    }
}
