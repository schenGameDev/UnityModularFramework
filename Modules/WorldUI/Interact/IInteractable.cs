using System.Collections.Generic;

namespace UnityModularFramework.Modules.WorldUI
{
    public interface IInteractable 
    {
        InteractMenuUI MenuUI { get; }
        InteractPromptUI PromptUI { get; }
        // user sees prompt before interaction
        string GetPrompt();
        List<InteractAction> GetAvailableActions(Interactor interactor);
        void OnInteractionStart(Interactor interactor);
        void OnInteractionEnd(Interactor interactor);
    }
}