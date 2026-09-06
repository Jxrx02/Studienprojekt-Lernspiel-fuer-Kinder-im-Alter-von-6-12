using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.GridMovement;

namespace TowerDefense.UI
{
    public class WallShopOverlay : MonoBehaviour
    {
        public static WallShopOverlay Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject root; // whole overlay
        [SerializeField] private Text titleText;
        [SerializeField] private Text priceText;
        [SerializeField] private Slider countSlider;
        [SerializeField] private Text countText;
        [SerializeField] private Button buyButton;
        [SerializeField] private KeyCode confirmKey = KeyCode.Return;

        [Header("Wall Settings")]
        [SerializeField] private int costPerTile = 25;

        private List<Vector3Int> currentGroup = new();
        private int builtCountInGroup = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuyClickedPublic);

            if (countSlider != null)
                countSlider.onValueChanged.AddListener(OnSliderChangedPublic);

            Hide();
        }

        private void OnDestroy()
        {
            if (buyButton != null)
                buyButton.onClick.RemoveListener(OnBuyClickedPublic);

            if (countSlider != null)
                countSlider.onValueChanged.RemoveListener(OnSliderChangedPublic);

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!root || !root.activeSelf)
                return;

            if (Input.GetKeyDown(confirmKey))
                OnBuyClickedPublic();
        }

        public void ShowGroup(List<Vector3Int> group, int builtCount)
        {
            if (group == null || group.Count == 0)
            {
                Hide();
                return;
            }

            currentGroup = new List<Vector3Int>(group);
            builtCountInGroup = builtCount;

            if (titleText != null)
                titleText.text = $"Wall-Gruppe ({group.Count} Tiles)";

            int maxToBuy = group.Count - builtCount;
            if (maxToBuy < 1) maxToBuy = 1;

            if (countSlider != null)
            {
                countSlider.minValue = 1;
                countSlider.maxValue = maxToBuy;
                if (countSlider.value < 1 || countSlider.value > maxToBuy)
                    countSlider.value = 1;
            }

            UpdatePriceDisplay();

            if (root != null)
                root.SetActive(true);
        }

        public void OpenPurchaseUI()
        {
            if (root != null)
                root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        // public so editor-created UI can bind directly
        public void OnSliderChangedPublic(float v)
        {
            UpdatePriceDisplay();
        }

        public void OnBuyClickedPublic()
        {
            int count = 1;
            if (countSlider != null)
                count = Mathf.RoundToInt(countSlider.value);

            // Select first 'count' non-built cells in the group
            List<Vector3Int> toBuild = new List<Vector3Int>();
            foreach (var c in currentGroup)
            {
                if (!GridManager.Instance.IsBuiltWallAtCell(c))
                    toBuild.Add(c);

                if (toBuild.Count >= count)
                    break;
            }

            if (toBuild.Count == 0)
                return;

            int totalPrice = toBuild.Count * costPerTile;
            if (LevelManager.instance.cur_coins < totalPrice)
            {
                Debug.Log("Nicht genug Geld!");
                return;
            }

            LevelManager.instance.cur_coins -= totalPrice;

            GridManager.Instance.BuildWallsAtCells(toBuild);

            Hide();
            TowerUI.Instance.UpdateUI();
        }

        private void UpdatePriceDisplay()
        {
            if (countText == null || priceText == null || countSlider == null)
                return;

            int count = Mathf.RoundToInt(countSlider.value);
            countText.text = $"{count}";
            int price = count * costPerTile;
            priceText.text = $"Preis: {price} Münzen";
        }
    }
}
