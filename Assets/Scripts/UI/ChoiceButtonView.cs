using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// A single choice button spawned dynamically by DialogueView.
    /// Attach to a Button prefab with a child TextMeshProUGUI label.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Button          _button;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
        }

        public void Setup(int choiceIndex, string text, Action<int> onSelected)
        {
            if (_label != null) _label.text = text;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onSelected(choiceIndex));
        }
    }
}
