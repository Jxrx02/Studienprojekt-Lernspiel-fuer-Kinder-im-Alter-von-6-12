using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.Wavesystem
{
    /// Sitzt auf dem World-Space-Canvas-Prefab, das über einem Spawnpoint
    /// anzeigt, welche Gegner dort als Nächstes spawnen.
    public class SpawnOverviewUI : MonoBehaviour
    {
        [SerializeField] private Text overviewText;

        public void Setup(Dictionary<string, int> enemyCounts)
        {
            var sb = new StringBuilder();
            bool first = true;

            foreach (var entry in enemyCounts)
            {
                if (!first) sb.Append("\n");
                sb.Append($"{entry.Value}x {entry.Key}");
                first = false;
            }

            if (overviewText != null)
                overviewText.text = sb.ToString();
        }
    }
}