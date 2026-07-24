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

    public void PlaySong(string song)
    {
        if (song == currentSong)
            return;
        currentSong = song;
        AudioManager.instance.PlaySFX(song);
    }

    public void CancelSong()
    {
        AudioManager.instance.CancelSFX(currentSong);
    }

    public void CancelSong(float dur)
    {
        AudioManager.instance.CancelSFXFadeOut(currentSong, dur);
    }
}
