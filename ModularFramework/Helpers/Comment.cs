#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using System;
using System.Linq;
using UnityEngine;
using S = UnityEngine.SerializeField;

/// <summary>
/// Use this component to leave comments to other developers (and yourself) on GameObjects, prefabs, etc.
/// Double-click the text area to edit.
/// </summary>
public sealed class Comment : MonoBehaviour
#if UNITY_EDITOR
    , ISerializationCallbackReceiver
#endif
{
    [S] string commentText;
    public string CommentText { set => commentText = value; }

    [S] int commentIconType;
    public IconType CommentIconType { set => commentIconType = (int) value; }

    public enum IconType
    {
        NoIcon,
        Info,
        Warning,
        WarningSoft,
        Error,
        ErrorSoft,
        Question,
        BugOk,
        BugWarning,
        LockRed,
        LockBlue,
        CrossRed,
        CrossBlue,
        CheckRed,
        CheckBlue,
        Star,
        Leaf,
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Comment))]
    [CanEditMultipleObjects]
    sealed class CommentEditor : Editor
    {
        static readonly GUILayoutOption[] closeButtonOptions = { GUILayout.MinWidth(128f), GUILayout.Height(18f) };
        static readonly GUILayoutOption[] iconTypeDropdownOptions = { GUILayout.MinWidth(128f) };
        static readonly Func<char,bool> newlineCountPredicate = x => x == '\n';
        static GUIContent commentGuiContent;
        static GUIStyle textEditingStyle;
        static GUIStyle textPreviewStyle;
        static Texture2D[] icons;

        SerializedProperty propertyText;
        SerializedProperty propertyIconType;
        bool editable;

        void OnEnable()
        {
            propertyText = serializedObject.FindProperty(nameof(commentText));
            propertyIconType = serializedObject.FindProperty(nameof(commentIconType));

            // set as editable by default if the comment is empty
            if (string.IsNullOrEmpty(propertyText.stringValue))
                editable = true;
        }

        static void SetupEditorStatics()
        {
            static Texture2D GetIcon(string id)
                => EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_" + id : id).image as Texture2D;

            commentGuiContent ??= new GUIContent { tooltip = "Double-click to edit the comment" };
            textEditingStyle ??= new GUIStyle(EditorStyles.textField) { wordWrap = true };
            textPreviewStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                padding = new RectOffset(12, 12, 8, 8),
                // change default font style and size here
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                // note that you can also use rich text in comments!
                richText = true,
            };
            icons ??= new []
            {
                GetIcon("console.infoicon.sml"),
                GetIcon("console.warnicon.sml"),
                GetIcon("console.warnicon.inactive.sml"),
                GetIcon("console.erroricon.sml"),
                GetIcon("console.erroricon.inactive.sml"),
                GetIcon("P4_Conflicted"),
                GetIcon("DebuggerAttached"),
                GetIcon("DebuggerEnabled"),
                GetIcon("P4_LockedLocal"),
                GetIcon("P4_LockedRemote"),
                GetIcon("P4_DeletedLocal"),
                GetIcon("P4_DeletedRemote"),
                GetIcon("P4_CheckOutLocal"),
                GetIcon("P4_CheckOutRemote"),
                GetIcon("Favorite Icon"),
                GetIcon("tree_icon_leaf"),
            };
        }

        public override void OnInspectorGUI()
        {
            SetupEditorStatics();

            if (editable)
            {
                serializedObject.Update();

                DrawEditableCommentGUI();

                if (target)
                    serializedObject.ApplyModifiedProperties();
            }
            else
            {
                DrawCommentGUI();
            }
        }

        void DrawEditableCommentGUI()
        {
            string text = propertyText.stringValue;
            int lineCount = text.Count(newlineCountPredicate);

            // draw editable comment text area
            {
                // no good way to estimate this, so we do it badly instead
                float textAreaHeight = EditorGUIUtility.singleLineHeight * 0.835f * (lineCount + 1.5f + text.Length / 32f);

                propertyText.stringValue = text = EditorGUILayout.TextArea(
                    text: text, 
                    style: textEditingStyle, 
                    options: GUILayout.MinHeight(textAreaHeight)
                );
            }

            EditorGUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());

            // draw message type selector
            propertyIconType.intValue = (int) (IconType) EditorGUILayout.EnumPopup(
                label: GUIContent.none,
                selected: (IconType) propertyIconType.intValue,
                iconTypeDropdownOptions
            );

            // draw the save/delete button
            if (string.IsNullOrEmpty(text))
            {
                if (GUILayout.Button("Delete", closeButtonOptions))
                    DestroyImmediate(target);
            }
            else
            {
                if (GUILayout.Button("Save", closeButtonOptions))
                    editable = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawCommentGUI()
        {
            var target = this.target as Comment;
            var currentEvent = Event.current;

            // draw non-editable comment text
            {
                commentGuiContent.text = target.commentText;
                commentGuiContent.image = target.commentIconType >= 1 && target.commentIconType < icons.Length + 1
                    ? icons[target.commentIconType - 1]
                    : null;

                EditorGUILayout.LabelField(
                    label: GUIContent.none,
                    label2: commentGuiContent,
                    style: textPreviewStyle,
                    options: Array.Empty<GUILayoutOption>()
                );
            }

            // begin editing comment on double-click
            {
                // require mouse in comment area
                if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
                    return;

                // require double-click
                if (currentEvent.clickCount < 2)
                    return;

                // set as editable and refresh inspector
                editable = true;
                Repaint();
            }
        }
    }

    /// <summary>
    /// Post processor that completely removes all Comment components from scenes.
    /// This callback is executed when entering play mode and during build.
    /// </summary>
    [PostProcessScene]
    static void RemoveCommentsFromScene()
    {
#if UNITY_2023_1_OR_NEWER
        foreach (var comment in FindObjectsByType<Comment>(FindObjectsInactive.Include, FindObjectsSortMode.None))
#else
        foreach (var comment in FindObjectsOfType<Comment>(includeInactive: true))
#endif
            DestroyImmediate(comment);
    }


    /// <summary>
    /// Serialization callback - strips comments from prefabs during build.
    /// We can't delete the component, but we can make it lighter by setting the text to null.
    /// </summary>
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        if (!BuildPipeline.isBuildingPlayer)
            return;

        commentText = null;
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() { }
#endif
}