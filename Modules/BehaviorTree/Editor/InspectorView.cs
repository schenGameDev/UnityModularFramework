using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
[UxmlElement]
public partial class InspectorView : VisualElement
{
    // public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> {}

    private Editor _editor;

    public InspectorView() {
        var styleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditorStyleSheet");
        styleSheets.Add(styleSheet);
    }

    internal void UpdateSelection(NodeView nodeView)
    {
        Clear();
        UnityEngine.Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(nodeView.Node);
        IMGUIContainer container = new (() => {
            if(_editor.target)
                _editor.OnInspectorGUI();
            });
        Add(container);
    }
}
