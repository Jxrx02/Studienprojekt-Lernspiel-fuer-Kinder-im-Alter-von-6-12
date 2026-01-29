using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class MusicScript : MonoBehaviour {


    [Header("")]
    public Slider MVolume;
    

    [Header("")]
    public AudioSource MusicBox;

    [Header("")]
    public AudioClip[] Musicclip;

    [Header("")]
    public Toggle musicToggle;
    public Text MusicTitel;

    public int playingSong = 0;

    private static MusicScript _instance;

    private int music;

    private void Awake()
    {
        #region CheckMusicBox
        //if we don't have an [_instance] set yet
        if (!_instance)
        {
            _instance = this;
        }
        //otherwise, if we do, kill this thing
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
        #endregion
        music = PlayerPrefs.GetInt("music");
        float savedVolume = PlayerPrefs.GetFloat("musicVolume", 1f);
        MVolume.value = savedVolume;
        MusicBox.volume = savedVolume;
        
        if (music == 0)
        {
            MusicBox.Play();
            musicToggle.isOn = true;
        }
        else
        {
            MusicBox.Stop();
            musicToggle.isOn = false;
        }
    }



    void LateUpdate()
    {
        if (MusicBox == null) return;
        if (music == 1)
        {
            MusicBox.Stop();
        }
        if (!MusicBox.isPlaying && music == 0)
        {
            MusicBox.Play();
        }
        if(MusicBox.clip == null)
        {
            SkipSong();
        }
        MusicBoolean();
    }
    

    public void SetVolume()
    {
        MusicBox.volume = MVolume.value;

    }
    public void SaveVolume()
    {
        PlayerPrefs.SetFloat("musicVolume",MVolume.value);
        PlayerPrefs.Save();
    }


    public void SkipSong()
    {
        if(playingSong == (Musicclip.Length -1))
        {
            playingSong = 0;
            playsong();
            return;
        }

        if (playingSong <= (Musicclip.Length -1))
        {
            playingSong += 1;
            playsong();
        }

    }

    public void playLastSong()
    {
        if(playingSong == 0)
        {
            playingSong = Musicclip.Length;
        }

        if (playingSong >= 0)
        {
            playingSong -= 1;
        }

        playsong();

    }

    void playsong()
    {
        MusicBox.clip = Musicclip[playingSong] as AudioClip;
        MusicBox.Play();
        MusicTitel.text = "Song: " + (MusicBox.clip.ToString().Replace("(UnityEngine.AudioClip)", ""));

        Debug.Log("Der Song " + MusicBox.clip + "wurde auf" +
            MusicBox.clip.ToString().Replace("(UnityEngine.AudioClip)", "") + " geparsed");

    }

    public void MusicBoolean()
    {
        if (musicToggle == null) return;
        if (musicToggle.isOn)
        {
            music = 0;
            PlayerPrefs.SetInt("music", 0);
        }

        if (!musicToggle.isOn)
        {
            music = 1;
            PlayerPrefs.SetInt("music", 1);
        }
    }

}
