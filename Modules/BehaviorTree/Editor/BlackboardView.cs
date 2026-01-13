using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class BlackboardView : VisualElement
{
    // public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> {}

    private Editor _editor;

    public BlackboardView() {
        var styleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditorStyleSheet");
        styleSheets.Add(styleSheet);
    }

    internal void UpdateBlackboard(BTBlackboard blackboard)
    {
        Clear();
        Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(blackboard);
        IMGUIContainer container = new (() => {
            if(_editor.target)
                _editor.OnInspectorGUI();
        });
        Add(container);
    }
}