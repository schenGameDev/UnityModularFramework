using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularFramework.Modules.BehaviorTree.Editor
{
    [UxmlElement]
    public partial class BlackboardView : VisualElement
    {
        // public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> {}

        private UnityEditor.Editor _editor;

        public BlackboardView()
        {
            var styleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditorStyleSheet");
            styleSheets.Add(styleSheet);
        }

        internal void UpdateBlackboard(BTBlackboard blackboard)
        {
            Clear();
            Object.DestroyImmediate(_editor);
            _editor = UnityEditor.Editor.CreateEditor(blackboard);
            InspectorElement inspector = new InspectorElement(_editor);
            Add(inspector);
        }
    }
}