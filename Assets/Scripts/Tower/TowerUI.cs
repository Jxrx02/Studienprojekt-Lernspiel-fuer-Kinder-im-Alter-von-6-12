using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    public class TowerUI:MonoBehaviour
    {
        public static TowerUI Instance { get; private set; }

        
        [Header("UI Elements")]
        [SerializeField] private Text txtTowerName; 
        [SerializeField] private Text txtTowerDesc; 

        [SerializeField] private Text txtLevel; 
        [SerializeField] private Slider experienceBar;
        [SerializeField] private Text experienceText;
        [SerializeField] private Text txtRange; 
        [SerializeField] private Text txtDps;       //Damage per second 
        [SerializeField] private Button btnNext; //target type
        [SerializeField] private Button btnPrev; //target type
        [SerializeField] public Text txtTargetPreference; 
       
        [SerializeField] private Button btnUpgradeSkill1; 
        [SerializeField] private Button btnUpgradeSkill2; 
        [SerializeField] private Button btnUpgradeSkill3;
        [SerializeField] private Button btnSell;
        [SerializeField] private Text txtSellTower;

        [SerializeField] private Text[] txtPathName;
        [SerializeField] private Text[] pathUpgradeCostTexts; 
        [SerializeField] private SpriteAnim[] pathShopIcon; 
        [SerializeField] private SpriteAnim shopImage; 

        [SerializeField] private Transform upgradePathIconIndicatorParent1;
        [SerializeField] private Transform upgradePathIconIndicatorParent2;
        [SerializeField] private Transform upgradePathIconIndicatorParent3;
        [SerializeField] private Sprite[] upgradePathIconIndicatorSprites = new Sprite[2]; 
        
        [SerializeField] private Button btnMoveHeroTo;

        [HideInInspector] public Tower selectedTower;
        
        private StatDiffDisplay statDiffDisplay;

        Image[] icons1;
        Image[] icons2;
        Image[] icons3;
        
        private void Awake()
        {
            // Singleton-Logik
            if (Instance == null)
            {
                Instance = this;
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject); 
            }
            
            icons1 = upgradePathIconIndicatorParent1.GetComponentsInChildren<Image>();
            icons2 = upgradePathIconIndicatorParent2.GetComponentsInChildren<Image>();
            icons3 = upgradePathIconIndicatorParent3.GetComponentsInChildren<Image>();
            

        }
        

        public void FocusTowerUI(Tower tower)
        {
            selectedTower = tower;
            btnMoveHeroTo.gameObject.SetActive(false);

            if (selectedTower is Hero hero)
            {
                btnMoveHeroTo.gameObject.SetActive(true);
                btnMoveHeroTo.onClick.AddListener(()=> hero.OnHeroMoveButtonPressed());
            }
            // Entferne vorhandene Event-Handler, bevor neue hinzugefügt werden
            btnNext.onClick.RemoveAllListeners();
            btnPrev.onClick.RemoveAllListeners();
            btnSell.onClick.RemoveAllListeners();
            btnUpgradeSkill1.onClick.RemoveAllListeners();
            btnUpgradeSkill2.onClick.RemoveAllListeners();
            btnUpgradeSkill3.onClick.RemoveAllListeners();


            // Event-Handler hinzufügen
            btnNext.onClick.AddListener(selectedTower.NextTargetType);
            btnPrev.onClick.AddListener(selectedTower.PreviousTargetType);
            btnSell.onClick.AddListener(() => tower.SellTower());
            btnUpgradeSkill1.onClick.AddListener(() => selectedTower.UpgradePath(0));
            btnUpgradeSkill2.onClick.AddListener(() => selectedTower.UpgradePath(1));
            btnUpgradeSkill3.onClick.AddListener(() => selectedTower.UpgradePath(2));
            
            UpdateUI();


        }

        public void UpdateUI()
        {
            if (!selectedTower || selectedTower.gameObject == null) return;

            txtTowerName.text = selectedTower.towerName;
            txtTowerDesc.text = selectedTower.towerDesc;
            txtLevel.text = $"Level:{selectedTower.level.ToString()}";
            txtRange.text = $"Range:{selectedTower.range.ToString()}";
            txtDps.text = $"Dmg:{selectedTower.damage.ToString()}";
            txtTargetPreference.text = selectedTower.targetPreference.ToString();
            shopImage.idle_sprites = selectedTower.GetComponent<SpriteAnim>().idle_sprites;
            txtSellTower.text = selectedTower.CalculateSellPrice() + " Coins";
            experienceBar.minValue = 0;
            experienceBar.maxValue = selectedTower.requiredExperience;
            experienceBar.value = selectedTower.experience;
            experienceText.text = selectedTower.experience + " / " + selectedTower.requiredExperience + " Exp";
            
            //UpdateUpgradePathIconsIndicator
            for (int i = 0; i < 5; i++)
            {
                icons1[i].sprite = i < selectedTower.pathLevels[0] ? upgradePathIconIndicatorSprites[1] : upgradePathIconIndicatorSprites[0];
                icons2[i].sprite = i < selectedTower.pathLevels[1] ? upgradePathIconIndicatorSprites[1] : upgradePathIconIndicatorSprites[0];
                icons3[i].sprite = i < selectedTower.pathLevels[2] ? upgradePathIconIndicatorSprites[1] : upgradePathIconIndicatorSprites[0];
            }
            
            for (int i = 0; i < selectedTower.upgradePaths.Length; i++)
            {
                int currentLevel = selectedTower.pathLevels[i];
                if (currentLevel < selectedTower.upgradePaths[i].levels.Length)
                {
                    int cost = selectedTower.upgradePaths[i].levels[currentLevel].upgradeCost;
                    pathUpgradeCostTexts[i].text = $"{cost} Gold";
                    txtPathName[i].text = selectedTower.upgradePaths[i].levels[currentLevel].upgradeName;
                    pathShopIcon[i].idle_sprites = selectedTower.upgradePaths[i].levels[currentLevel].pathShopIcon;
                }
                else
                {
                    pathUpgradeCostTexts[i].text = $"Maximal!";
                }
            }
            
            //Lock purchase if locked in research
            for (int i = 0; i < 3; i++)
            {
                int currentLevel = selectedTower.pathLevels[i];

                if (currentLevel < selectedTower.upgradePaths[i].levels.Length)
                {
                    var upgrade = selectedTower.upgradePaths[i].levels[currentLevel];
                    string towerName = selectedTower.towerName;
                    string pathName = selectedTower.upgradePaths[i].towerName;
                    string upgradeName = upgrade.upgradeName;

                    bool isUnlocked = TowerDefense.Research.ResearchManager.Instance
                        .unlockedResearch.Any(t =>
                            t.researchedPaths.Any(p => p.pathName.Contains(pathName)) &&
                            t.researchedPaths.Any(p =>
                                p.unlockedUpgrades.Contains(upgradeName)
                            )
                        );

                    switch (i)
                    {
                        case 0: btnUpgradeSkill1.interactable = isUnlocked; break;
                        case 1: btnUpgradeSkill2.interactable = isUnlocked; break;
                        case 2: btnUpgradeSkill3.interactable = isUnlocked; break;
                    }
                }
            }




        }

    }
}