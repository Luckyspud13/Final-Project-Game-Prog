using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.Controls;
[RequireComponent(typeof(AudioSource))]
public class LevelManager : MonoBehaviour
{
    AudioSource audioSource;
    AudioClip WinSFX;
    AudioClip LoseSFX;
    public AudioClip LevelMusic;
    public String nextLevel;
    float counter;
    Boolean isPlaying;
    Boolean isLost;
    Boolean isWon;
    public GameObject player;
    AudioSource playerAudio;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        isPlaying = true;
        isLost = false;
    }

    void Start()
    {
        // tracks how long the level has taken
        counter = 0;
        // play the level music
        playerAudio = player.GetComponent<AudioSource>();
        playerAudio.loop = true;
        playerAudio.clip = LevelMusic;
        playerAudio.Play();
    }

    
    void Update()
    {
        TimeTick();
        if(isWon)
        {
            // ChangeMusic(WinSFX);
            // if a next level is given, move on, otherwise replay
            if(nextLevel != null)
            {
                NextLevel(nextLevel);
            }
            else
            {
                ReplayLevel();
            }
            
        }
        if(isLost)
        {
            ChangeMusic(LoseSFX);
            Invoke("ReplayLevel", 1);
        }
    }

    void TimeTick()
    {
        counter += Time.deltaTime;
    }

    // is called if the player determines they have lost the level
    public void LevelLost()
    {
        isLost = true;
        isPlaying = false;
    }

    // called if player determines the level has been won
    public void IsBeaten()
    {
        isWon = true;
        isPlaying = false;
    }

    // plays the input clip
    void PlayAudioClip(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    // calls the given scene
    void NextLevel(String level)
    {
        SceneManager.LoadScene(level);
    }

    // restarts the current scene
    void ReplayLevel()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    void ChangeMusic(AudioClip clip)
    {
        playerAudio.loop = false;
        playerAudio.clip = clip;
        playerAudio.Play();
    }
}
