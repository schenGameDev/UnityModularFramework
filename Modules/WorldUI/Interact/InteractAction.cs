using System;

namespace UnityModularFramework.Modules.WorldUI
{
    public class InteractAction
    {
        public string Name {get; private set;}
        public string UnavailableHint {get; private set;}
        public Action<Interactor> Execute {get; private set;}
        
        private readonly Func<Interactor, bool> _isAvailable;


        public InteractAction(string name, string unavailableHint, Func<Interactor, bool> isAvailable, Action<Interactor> execute)
        {
            Name = name;
            UnavailableHint = unavailableHint;
            _isAvailable = isAvailable;
            Execute = execute;
        }
        
        public bool CanExecute(Interactor interactor) => _isAvailable == null || _isAvailable(interactor);
    }
}