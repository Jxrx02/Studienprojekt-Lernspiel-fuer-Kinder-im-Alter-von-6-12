using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    [RequireComponent(typeof(Tower))]
    public class StatDiffDisplay: MonoBehaviour
    {
        [SerializeField] private GameObject statTextPrefab;
        [SerializeField] private int poolSize = 15;

        private List<Text> pool = new List<Text>();

        private void Awake()
        {
            var statDiffCanvas = GetComponentInChildren<Canvas>();
            if (statDiffCanvas != null)
            {
                for (int i = 0; i < poolSize; i++)
                {

                    var go = Instantiate(statTextPrefab, statDiffCanvas.transform);
                    go.SetActive(false);
                    pool.Add(go.GetComponent<Text>());
                }
            }
        }

        public void ShowDiff(string statName, float oldValue, float newValue)
        {
            Text textObj = GetFreeText();
            if (textObj == null) return;

            textObj.gameObject.SetActive(true);
            StartCoroutine(AnimateDiff(textObj, statName, oldValue, newValue));
        }

        private Text GetFreeText()
        {
            foreach (var txt in pool)
            {
                if (!txt.gameObject.activeInHierarchy)
                    return txt;
            }
            return null;
        }

        private IEnumerator AnimateDiff(Text text, string statName, float oldValue, float newValue)
        {
            float diff = newValue - oldValue;
            if (Mathf.Approximately(diff, 0f))
            {
                text.gameObject.SetActive(false);
                yield break;
            }

            text.gameObject.SetActive(true);

            float duration = 1f;
            float elapsed = 0f;

            RectTransform rect = text.GetComponent<RectTransform>();
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + Vector2.down * 4f;   // 40 Pixel nach unten

            Color startColor = text.color;
            Color endColor = startColor;
            endColor.a = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                float currentValue = Mathf.Lerp(oldValue, newValue, t);
                float shownDiff = currentValue - oldValue;

                text.text = $"{(shownDiff >= 0 ? "+" : "")}{shownDiff:F1} {statName}";

                // Nach unten bewegen
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                // Ausblenden
                text.color = Color.Lerp(startColor, endColor, t);

                yield return null;
            }

            text.text = $"{(diff >= 0 ? "+" : "")}{diff:F1} {statName}";

            // Ausgangszustand wiederherstellen
            rect.anchoredPosition = startPos;
            text.color = startColor;
            text.gameObject.SetActive(false);
        }

    }
}