using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    /// <summary>
    /// A queue of messageCards, messageCards can be chat messages or choices
    /// </summary>
    [RequireComponent(typeof(Marker))]
    public class WaterfallQueue : TextPrinterBase, ISavable, ISelectableGroup, IMark
    {
        [SerializeField] private int maxCards = 100;
        [SerializeField] private ChoiceCard choicePrefab;
        [SerializeField] private Image characterImage;
        [SerializeField] private GameObject characterImageContainer;
        [SerializeField] private GameObject endIndicator;
        [SerializeField] private Button nextLineButton;
        [SerializeField] private CharacterBucket characterBucket;

        private readonly List<Transform> _cards = new();
        private LineCard _latestLineCard;
        private Action _callback;
        [SavableState(nameof(SaveHistory))] private string _historyStr = "";


        private void Awake()
        {
            characterImage.sprite = null;
            characterImageContainer.SetActive(false);
            foreach (var b in _cards)
            {
                Destroy(b);
            }

            _historyStr = "";
            _cards.Clear();
        }

        private LineCard CreateLineCard(CharacterDatum datum)
        {
            var card = Instantiate(datum.lineCard, transform);
            card.PrefabKey = datum.id;
            return card;
        }

        private ChoiceCard CreateChoiceCard()
        {
            return Instantiate(choicePrefab, transform);
        }

        private void AddNewCard(LineCard card)
        {
            _cards.Add(card.transform);
            _latestLineCard = card;
            CheckCardLength();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }
        
        private void AddNewCard(ChoiceCard card)
        {
            _cards.Add(card.transform);
            _latestLineCard = null;
            CheckCardLength();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        private void CheckCardLength()
        {
            if (_cards.Count > maxCards)
            {
                var card = _cards.RemoveAtAndReturn(0);
                card.gameObject.SetActive(false);
                Destroy(card.gameObject);
            }
        }

        private void RemoveLastChoiceCard()
        {
            if (_cards.Count == 0)
            {
                return;
            }

            int lastChoiceIndex = -1;
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                if (_cards[i].TryGetComponent<ChoiceCard>(out _))
                {
                    lastChoiceIndex = i;
                    break;
                }
            }

            if (lastChoiceIndex < 0)
            {
                return;
            }

            var card = _cards.RemoveAtAndReturn(lastChoiceIndex);
            card.gameObject.SetActive(false);
            Destroy(card.gameObject) ;
            _latestLineCard = _cards.Count > 0 && _cards[^1].TryGetComponent<LineCard>(out var lineCard) ? lineCard : null;
        }

        private void OnCardComplete()
        {
            Done = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            _callback?.Invoke();
        }

        private string SaveHistory()
        {
            if (_cards.IsEmpty()) return "";
            StringBuilder sb = new ();
            bool notEmpty = false;
            foreach (var card in _cards)
            {
                if (card.TryGetComponent<LineCard>(out var lineCard))
                {
                    continue;
                }
                if (notEmpty)
                {
                    sb.Append("||");
                }
                else
                {
                    notEmpty = true;
                }
                sb.Append(lineCard.PrefabKey);
                sb.Append("&&");
                sb.Append(lineCard.textbox.text);
            }
            
            return sb.ToString();
        }

        #region Print
        public override void Print(string text, Action callback, params string[] parameters)
        {
            Done = false;
            string characterCode = parameters.Length == 0 || parameters[0].IsEmpty() ? "default" : parameters[0];
            var opt = characterBucket.Get(characterCode);
            if (opt.IsEmpty)
            {
                Debug.LogWarning($"Character code {characterCode} not found in bucket {characterBucket.name}");
            }
            var datum = opt.Get();
            if (datum != null && !string.IsNullOrEmpty(datum.name))
            {
                text = $"{datum.name}: {text}";
            }
            
            LineCard card = CreateLineCard(datum);
            card.ReturnEarly = ReturnEarly;
            endIndicator?.SetActive(false);
            nextLineButton?.gameObject.SetActive(true);
            card.Print(text, () =>
            {
                endIndicator?.SetActive(true);
                OnCardComplete();
            });
            _callback = callback;
            AddNewCard(card);
            
            // speaker image
            
            if (datum != null && datum.sprite != null)
            {
                characterImage.sprite = datum.sprite;
                characterImage.color = characterImage.color.SetAlpha(1);
                characterImageContainer.SetActive(true);
            }
            else
            {
                // characterImage.color = characterImage.color.SetAlpha(0);
            }
        }

        public override void Skip()
        {
            if (_latestLineCard != null && !_latestLineCard.Done)
            {
                _latestLineCard.Skip();
            }
        }

        public override void Clean()
        {
            ReturnEarly = false;
            Done = false;
            _latestLineCard = null;
        }
        

        #endregion
        
        #region Choice

        public string ChoiceGroupName => printerName;
        public bool EnableOnAwake => !hideWhenNotUsed;
        public void Activate(InkChoice choiceInfo, bool showHiddenChoice)
        {
            gameObject.SetActive(true);
            endIndicator?.SetActive(false);
            nextLineButton?.gameObject.SetActive(false);
            ChoiceCard card = CreateChoiceCard();
            card.OnSelect = Select;
            card.Activate(choiceInfo, showHiddenChoice);
            AddNewCard(card);
            _callback = null;
        }

        public void Select(int index)
        {
            OnSelect?.Invoke(index);
            RemoveLastChoiceCard();
            OnCardComplete();
        }

        public Action<int> OnSelect { get; set; }
        
        public void ResetState() => Clean();
        #endregion

        #region ISavable

        public string Id => printerName;

        public virtual void Load()
        {
            if (gameObject.activeSelf && _historyStr.Length > 0)
            {
                _historyStr.Split("||").ForEach(line =>
                {
                    var arr = line.Split("&&");
                    if (arr.Length != 2)
                    {
                        Debug.LogError($"Incorrect number of card parameter: {arr.Length} in {arr}");
                        return;
                    }
                    string characterCode = arr[0];
                    var datum = characterBucket.Get(characterCode).Get();
        
                    var card = CreateLineCard(datum);
                    card.textbox.text = arr[1];
                    _cards.Add(card.transform);
                });
            }
        }
        #endregion

        #region IRegistrySO

        public List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes)
        {
            if (alreadyRegisteredTypes.Contains(typeof(InkUIIntegrationSO))) return new();
            SingletonRegistry<InkUIIntegrationSO>.Instance?.Register(transform);
            return new() { typeof(InkUIIntegrationSO) };
        }

        public void UnregisterSelf()
        {
            SingletonRegistry<InkUIIntegrationSO>.Instance?.Unregister(transform);
        }

        #endregion
    }
}