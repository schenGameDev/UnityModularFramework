using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class InspectorView : VisualElement
{
    // public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> {}

    private Editor _editor;
    private NodeView _currentNodeView;

    public InspectorView() {
        var styleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditorStyleSheet");
        styleSheets.Add(styleSheet);
    }

    internal void UpdateSelection(NodeView nodeView)
    {
        Clear();
        Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(nodeView.Node);
        InspectorElement inspector = new InspectorElement(_editor);
    
        _currentNodeView = nodeView;

        var resetButton = new Button(ResetNode) { text = "Reset" };
        Add(resetButton);

        Add(inspector);
    }
    
    private void ResetNode()
    {
        if (_currentNodeView?.Node == null) return;
        
        Undo.RecordObject(_currentNodeView.Node, "Reset Node");
        
        var node = _currentNodeView.Node;
        var type = node.GetType();
        var newInstance = ScriptableObject.CreateInstance(type) as BTNode;
        newInstance.Initialize(node.guid);
        
        EditorUtility.CopySerialized(newInstance, node);
        ScriptableObject.DestroyImmediate(newInstance);
        
        EditorUtility.SetDirty(node);
        AssetDatabase.SaveAssets();
        if(_editor) Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(node);
    }
}