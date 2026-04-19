using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuTransition : MonoBehaviour
{
    public Button[] buttons;
    public TMP_Text text;
    public Slider slider;

    void Awake()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        for(int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }
        for(int i = 0; i < unlockedLevel; i++)
        {
            buttons[i].interactable = true;
        }

        slider.value = PlayerPrefs.GetFloat("MouseSensitivity", 100);
        text.text = slider.value.ToString();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenLevel(int levelId)
    {
        string levelName = "Level" + levelId;
        SceneManager.LoadScene(levelName);
    }

    public void SetMouseSensitivity()
    {
        text.text = slider.value.ToString();
        PlayerPrefs.SetFloat("MouseSensitivity", slider.value);
    }
}
