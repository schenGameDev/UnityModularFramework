using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

public class BehaviorTreeEditor : EditorWindow
{
    BehaviorTreeView _treeView;
    InspectorView _inspectorView;

    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

    [SerializeField] private StyleSheet m_StyleSheet=default;

    [MenuItem("BehaviorTreeEditor/Editor")]
    public static void OpenWindow()
    {
        BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
        wnd.titleContent = new GUIContent("BehaviorTreeEditor");
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line) {
        if(Selection.activeObject is BehaviorTreeSO) {
            OpenWindow();
            return true;
        }
        return false;
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        if(m_VisualTreeAsset==null) m_VisualTreeAsset = Resources.Load<VisualTreeAsset>("BehaviorTreeEditor");
        if(m_StyleSheet==null) m_StyleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditorStyleSheet");

        m_VisualTreeAsset.CloneTree(root);
        root.styleSheets.Add(m_StyleSheet);

        _treeView = root.Q<BehaviorTreeView>();
        _inspectorView = root.Q<InspectorView>();
        _treeView.OnNodeSelected = OnNodeSelectionChanged;
        OnSelectionChange();
    }

    private void OnSelectionChange() {
        BehaviorTreeSO tree = Selection.activeObject as BehaviorTreeSO;
        if(!tree) {
            if(Selection.activeGameObject) {
                BTMarker marker = Selection.activeGameObject.GetComponent<BTMarker>();
                if(marker) {
                    tree = marker.tree;
                }
            }
        }

        if(tree && (Application.isPlaying || AssetDatabase.CanOpenAssetInEditor(tree.GetInstanceID()))) {
            _treeView.PopulateView(tree);
        }
    }

    private void OnNodeSelectionChanged(NodeView node) {
        _inspectorView.UpdateSelection(node);
    }

    private void OnEnable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }


    private void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        switch (change)
        {
            case PlayModeStateChange.EnteredEditMode:
                OnSelectionChange();
                break;
            case PlayModeStateChange.ExitingEditMode:
                break;
            case PlayModeStateChange.EnteredPlayMode:
                OnSelectionChange();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
            default:
                break;
        }
    }

    private void OnInspectorUpdate() {
        _treeView?.UpdateNodeState();
    }
}
