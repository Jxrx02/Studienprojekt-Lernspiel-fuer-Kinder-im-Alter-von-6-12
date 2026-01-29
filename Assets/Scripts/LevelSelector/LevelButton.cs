namespace TowerDefense
{
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public int levelIndex;
    
    public Sprite unlockedSprite;
    public Sprite lockedSprite;
    void Start()
    {
        Button button = GetComponent<Button>();
        Image buttonImage = GetComponent<Image>();

        if (levelIndex <= LevelUnlocker.GetUnlockedLevel())
        {
            buttonImage.sprite = unlockedSprite;

            button.interactable = true;
        }
        else
        {
            buttonImage.sprite = lockedSprite;
            button.interactable = false;
        }

    }

    public void DeleteProgress()
    {
        PlayerPrefs.DeleteKey("unlockedLevel");
    }

    public void UnlockAllLvls()
    {
        PlayerPrefs.SetInt("unlockedLevel", 10);
        PlayerPrefs.Save();

    }

}

}