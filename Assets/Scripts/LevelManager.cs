namespace TowerDefense
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    public class LevelManager : MonoBehaviour
    {
        public static LevelManager instance;

        [Header("current Game-Variables")] private bool doubleTime;
        public int cur_coins;
        public int start_coins = 35;
        public int cur_health;
        public int start_health = 20;

        [Header("GameObjctlinking ")] public GameObject pauseCanvas;
        public GameObject gameCanvas;
        public GameObject deathScreen;
        public GameObject questionPanel;
        public GameObject endScreen;

        public Text txt_money;
        public Text txt_health;
        public GameObject towerUIPrefab;
        public GameObject waveManager;
        public GameObject clickToStartGameObject;
        [SerializeField] private DialogCanvas dialogCanvasPrefab;

        [Header("Texturelinking ")] public Sprite img_doubleTime;
        public Sprite img_normspeed;
        public GameObject btn_image_fastForward;

        public Sprite img_pause;
        public Sprite img_play;
        public GameObject btn_image_pause;

        public Boolean heroFielded;


        void Start()
        {
            Actions.onEnemyReachedEnd += LoseHealth;
            Actions.onEnemyDeath += GainCoins;
            Actions.onLvlComplete += LvlCompleted;
            Actions.onWaveSpawnComplete += OnWaveSpawnComplete ;

            cur_health = start_health;
            cur_coins = start_coins;
            UpdateStats();
// StartCoroutine(GetComponent<SceneFadeIn>().Fade(true));

            if (TowerUI.Instance == null)
            {
                Instantiate(towerUIPrefab);
            }

            if (DialogCanvas.instance == null)
            {
                Instantiate(dialogCanvasPrefab);
            }

            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            OnStartWaveClick();
        }

        private Boolean allwavesSpawned = false;
        private void OnWaveSpawnComplete()
        {
            allwavesSpawned = true;
        }

        private void LvlCompleted()
        {
            if(!allwavesSpawned) return;
            FindAnyObjectByType<LevelUnlocker>().CompleteLevel();
            Debug.Log("nächstes lvl freigeschaltet");
            endScreen.gameObject.SetActive(true);
        }

        public void OnStartWaveClick()
        {
            OneClickInWorldListener.ListenOnce((Vector3 pos) =>
            {
                clickToStartGameObject.SetActive(false);
                waveManager.SetActive(true);

                Vector3 dummy = pos;
                Debug.Log("Starting game");
            });
        }

        public Boolean CanPurchase(int price)
        {
            if ((cur_coins - price) < 0) return false;
            else return true;
        }

        public Boolean DoPurchase(int price)
        {
            if (CanPurchase(price) == false) return false;
            cur_coins -= price;
            UpdateStats();
            return true;
        }

private void UpdateStats()
{
    if (txt_money != null)
        txt_money.text = cur_coins.ToString();
    else
        Debug.LogWarning("txt_money ist zerstört oder nicht zugewiesen!");

    if (txt_health != null)
        txt_health.text = cur_health.ToString();
    else
        Debug.LogWarning("txt_health ist zerstört oder nicht zugewiesen!");
}


        public void Button_DoubleTime()
        {
            doubleTime = !doubleTime;
            if (doubleTime)
            {
                btn_image_fastForward.GetComponent<Image>().sprite = img_doubleTime;
                Time.timeScale = 3;

            }
            else
            {
                btn_image_fastForward.GetComponent<Image>().sprite = img_normspeed;
                Time.timeScale = 1;

            }
        }

        public void Pause()
        {
            btn_image_pause.GetComponent<Image>().sprite = img_pause;
            pauseCanvas.gameObject.SetActive(true);

            Time.timeScale = 0;

        }

        public void UnPause()
        {
            pauseCanvas.gameObject.SetActive(false);

            if (doubleTime)
                Time.timeScale = 3;
            else
                Time.timeScale = 1;

            btn_image_pause.GetComponent<Image>().sprite = img_play;

        }

        public void LoseHealth(GameObject enemy)
        {
            if (cur_health > 0)
                cur_health -= enemy.GetComponent<Enemy>().currentHealth;
            else
            {
                ShowDeathScreen();
            }

            UpdateStats();
        }

        public void GainCoins(GameObject enemy)
        {
            cur_coins += enemy.GetComponent<Enemy>().enemyConfig.goldReward;
            UpdateStats();
        }

        public void ShowDeathScreen()
        {
            deathScreen.gameObject.SetActive(true);
            Time.timeScale = 0;

        }
       

        public void ReviveFailed()
        {
            Time.timeScale = 1;
            questionPanel.gameObject.SetActive(false);
            deathScreen.GetComponentInChildren<Button>().interactable = false;
            deathScreen.gameObject.SetActive(true);

        }
        public void DoRevive()
        {
            //entfere die letzten 3 gegner

            for (int i = 0; i < 6; i++)
            {
                try
                {
                    GameObject enemy = TowerHeroManager.instance.enemies[0];
                    TowerHeroManager.instance.UnregisterEnemy(enemy);
                    Destroy(enemy);
                }
                catch
                {

                }

            }

            cur_health += 5;
            UpdateStats();
            deathScreen.gameObject.SetActive(false);
            questionPanel.gameObject.SetActive(false);

            Time.timeScale = 1;
        }
    }
}