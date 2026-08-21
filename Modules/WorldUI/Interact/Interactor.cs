using UnityEngine;

namespace UnityModularFramework.Modules.WorldUI
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private float range = 2.5f;
        [SerializeField] private LayerMask interactMask;
        
        [SerializeField, Min(8)] private int maxOverlapHits = 16;
        
        public bool InInteraction { get; private set; }
        
        private IInteractable _current;
        private Collider[] _overlapHits;
        private bool _inMenu;
        
        private InteractPromptUI ActivePromptUI => _current?.PromptUI;
        private InteractMenuUI ActiveMenuUI => _current?.MenuUI;

        #region Lifecycle
        private void Awake() {
            _overlapHits = new Collider[Mathf.Max(8, maxOverlapHits)];
            
        }
        
        private void Update() {
            if (_inMenu) {
                return;
            }
            
            DetectTarget();
        }
        #endregion
        
        private void DetectTarget() {
            IInteractable nearest = null;
            float best = range * range;
            var hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, _overlapHits, interactMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++) {
                var hit = _overlapHits[i];
                if (!hit) continue;
                
                var interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || interactable.PromptUI == null) continue;
                
                var target = (interactable as Component).transform;
                float sqr = (target.position - transform.position).sqrMagnitude;
                if (sqr >= best) continue;
                
                best = sqr;
                nearest = interactable;
            }

            if (nearest == _current) {
                // same active interactable
                return;
            }
            // new interactable
            ActivePromptUI?.Hide();
            _current = nearest;
            if (_current != null)
            {
                ActivePromptUI?.ShowPrompt(_current.GetPrompt(), StartInteract);
                SetPromptPosition();
            }
        }

        private void SetPromptPosition()
        {
            if (ActivePromptUI != null && ActivePromptUI.isWorldUI && ActiveMenuUI.transform.parent != ((Component) _current).transform)
            {
                ActivePromptUI.transform.SetParent(((Component) _current).transform);
                ActiveMenuUI.transform.localPosition = Vector3.zero;
            }
        }
        
        private void StartInteract() {
            ActivePromptUI?.Hide();
            if (_current == null) return;
            EnterMenu();
        }

        private void EnterMenu() {
            _inMenu = true;
            InInteraction = true;
            _current.OnInteractionStart(this);
            ActiveMenuUI?.ShowActions(this, _current);
        }

        public void ExitMenu() {
            if (!_inMenu) return;
            
            _current.OnInteractionEnd(this);
            _inMenu = false;
            InInteraction = false;
            ActiveMenuUI?.Hide();
            _current = null;
        }

    }
}