using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using ScriptableObjects;

namespace TowerDefense.Research
{
    public class ResearchMenu : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject towerSelectionPanel;
        public Transform towerListParent; // Hier kommen alle Turm-Buttons rein
        public GameObject upgradeSelectionPanel;
        public Transform upgradeListParent; // Hier kommen alle Upgrade-Infos rein

        [Header("Prefabs")]
        public GameObject towerButtonPrefab; // Button + Icon für Türme
        public GameObject upgradeEntryPrefab; // Upgrade-Infos

        [Header("Other UI")]
        private TowerResearchInfo selectedTower;
        

        private void Start()
        {
            ShowTowerSelection();
        }

        private void ShowTowerSelection()
        {
            // Vorher alles alte löschen
            foreach (Transform child in towerListParent)
                Destroy(child.gameObject);

            foreach (var tower in ResearchManager.Instance.availableTowers)
            {
                CreateTowerButton(tower);
            }
            towerSelectionPanel.SetActive(true);
        }

        private void CreateTowerButton(TowerResearchInfo towerInfo)
        {
            var obj = Instantiate(towerButtonPrefab, towerListParent);
            var button = obj.GetComponent<Button>();
            var texts = obj.GetComponentsInChildren<Text>();
            var image = obj.GetComponentInChildren<SpriteAnim>();

            texts[0].text = towerInfo.towerName;
            image.idle_sprites = towerInfo.sprites; // Holt das Sprite vom Turm

            button.onClick.AddListener(() =>
            {
                selectedTower = towerInfo;
                ShowUpgradeSelection();
            });
        }

        private void ShowUpgradeSelection()
        {
            // Vorher alles alte löschen
            foreach (Transform child in upgradeListParent)
                Destroy(child.gameObject);

            if (selectedTower != null)
            {
                foreach (var path in selectedTower.upgradePaths)
                {
                    foreach (var upgrade in path.levels)
                    {
                        CreateUpgradeEntry(selectedTower.towerName, path.towerName, upgrade);
                    }
                }
            }
        }

        private void CreateUpgradeEntry(string towerName, string pathName, TowerUpgradeLevel upgrade)
        {
            var obj = Instantiate(upgradeEntryPrefab, upgradeListParent);

            var texts = obj.GetComponentsInChildren<Text>();

            // Text 0 = Name, Text 1 = Kosten + Beschreibung, Text 2 = Stats
            texts[0].text = upgrade.upgradeName;
            texts[1].text = $"{upgrade.description}";
            texts[2].text = GenerateStatString(upgrade);

            var button = obj.GetComponentInChildren<Button>();

            bool isUnlocked = ResearchManager.Instance.IsUpgradeUnlocked(towerName, pathName, upgrade.upgradeName);

            button.interactable = !isUnlocked;
            button.GetComponentInChildren<Text>().text = isUnlocked ? "[Freigeschaltet]" : $"Freischalten - {upgrade.researchCost}";

            button.onClick.AddListener(() =>
            {
                TryResearchUpgrade(towerName, pathName, upgrade);
            });
            
        }

        private string GenerateStatString(TowerUpgradeLevel upgrade)
        {
            string stats = "";

            if (upgrade.damageIncrease != 0)
                stats += $"+{upgrade.damageIncrease} Schaden\n";
            if (upgrade.rangeIncrease != 0)
                stats += $"+{upgrade.rangeIncrease:F1} Reichweite\n";
            if (upgrade.attackSpeedIncrease != 0)
                stats += $"+{upgrade.attackSpeedIncrease:F1} AS\n";
            if (upgrade.timeInBetweenShotsDecrease != 0)
                stats += $"-{upgrade.timeInBetweenShotsDecrease:F1} Cooldown\n";

            if (upgrade.coinsEarnedPerSecond != 0)
                stats += $"+{upgrade.coinsEarnedPerSecond:F1} Münzen/Sek\n";
            if (upgrade.healthRegenPerSecond != 0)
                stats += $"+{upgrade.healthRegenPerSecond:F1} Heilung/Sek\n";

            if (upgrade.rangeMultiplier != 0)
                stats += $"x{upgrade.rangeMultiplier:F2} Reichweite\n";
            if (upgrade.dmgMultiplier != 0)
                stats += $"x{upgrade.dmgMultiplier:F2} Schaden\n";
            if (upgrade.attackSpeedMultiplier != 0)
                stats += $"x{upgrade.attackSpeedMultiplier:F2} Feuerrate\n";
            if (upgrade.slowMultiplier != 0)
                stats += $"x{upgrade.slowMultiplier:F2} Verlangsamung\n";

            return stats.TrimEnd();
        }

        private void TryResearchUpgrade(string towerName, string pathName, TowerUpgradeLevel upgrade)
        {
            if (ResearchManager.Instance.CanAffordResearch(upgrade.researchCost))
            {
                ResearchManager.Instance.UnlockUpgrade(towerName, pathName, upgrade.upgradeName);
                ResearchManager.Instance.SpendResearchPoints(upgrade.researchCost);
                ShowUpgradeSelection(); // Seite neu laden
            }
            else
            {
                Debug.LogWarning("Nicht genug Forschungspunkte!");
            }
        }
        

    }
}
