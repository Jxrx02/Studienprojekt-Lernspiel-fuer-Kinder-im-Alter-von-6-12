namespace TowerDefense.Research
{
    using UnityEngine;
    using UnityEngine.UI;

    public class ConfirmPopupManager : MonoBehaviour
    {
        public GameObject confirmPopup;   // Das Popup-Panel
        public Button yesButton;          // Ja-Button
        public Button noButton;           // Nein-Button
        public Text confirmText;          // Der Text (z.B. "Bist du sicher?")

        private System.Action onConfirm;  // Die Aktion, die ausgeführt wird, wenn bestätigt wird

        private void Start()
        {
            // Buttons setzen
            yesButton.onClick.AddListener(OnYesClicked);
            noButton.onClick.AddListener(OnNoClicked);

            // Popup initial ausblenden
            confirmPopup.SetActive(false);
        }

        // Wird aufgerufen, um das Popup anzuzeigen
        public void ShowConfirmPopup(string message, System.Action onConfirmAction)
        {
            confirmText.text = message;
            onConfirm = onConfirmAction;
            confirmPopup.SetActive(true); // Popup sichtbar machen
        }

        private void OnYesClicked()
        {
            onConfirm?.Invoke();  // Bestätigungsmethode aufrufen
            HidePopup();  // Popup ausblenden
        }

        private void OnNoClicked()
        {
            HidePopup();  // Popup ausblenden ohne zu bestätigen
        }

        private void HidePopup()
        {
            confirmPopup.SetActive(false);
        }
    }

}