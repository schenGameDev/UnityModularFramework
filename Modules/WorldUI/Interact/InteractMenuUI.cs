using ModularFramework;
using UnityEngine;

namespace UnityModularFramework.Modules.WorldUI
{
    /// <summary>
    /// A UI may be shared among IInteractable. Show prompt -> show actions -> user select action -> execute action.
    /// </summary>
    public class InteractMenuUI : MonoBehaviour, IResetable
    {
        [SerializeField] private InteractButton[] options;
        
        private InteractButton _current;

        private void Awake()
        {
            ResetState();
        }
        
        public void ShowActions(Interactor interactor, IInteractable interactable)
        {
            var actionItr = interactable.GetAvailableActions(interactor).GetEnumerator();
            foreach (var ui in options) {
                if (actionItr.MoveNext())
                {
                    ui.Show(() =>
                    {
                        actionItr.Current?.Execute(interactor);
                        interactor.ExitMenu();
                    });
                    continue;
                }
                ui.Hide();
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ResetState();
        }

        public void ResetState()
        {
            foreach (var ui in options) {
                ui.Hide();
            } 
        }
    }
}