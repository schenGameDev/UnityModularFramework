using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Utility;
using TMPro;
using UnityEngine;

public class Selectable : MonoBehaviour, ILive
{
    public int index;
    public string text;
    public bool needConfirm;
    [ShowField(nameof(needConfirm))] public ConfirmationGroup confirmation;
    [ShowField(nameof(needConfirm))] public bool hideGroupWhenConfirm = true;
    [ShowField(nameof(needConfirm))] public string confirmationText="";
    [field: SerializeField] public bool Live { get; set; }
    
    [ReadOnly] public bool hasSelected = false;

    [SerializeField,Child(Flag.Optional | Flag.IncludeInactive)] protected TextMeshProUGUI tmp;
    [SerializeField,Parent(Flag.Optional | Flag.IncludeInactive)] private SelectableGroup selectableGroup;
        
#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
    protected virtual void Awake()
    {
        if (!selectableGroup)
        {
            DebugUtil.Error("SelectableGroup not found in parent");
        }
    }
    public virtual void Select() {
        if(!Live && hasSelected) return;
        if (needConfirm)
        {
            if(!confirmation) confirmation = ConfirmationGroup.Instance;
            confirmation.Activate(confirmationText, ConfirmSelection);
            if(hideGroupWhenConfirm) Hide(true);
            return;
        } 
        ConfirmSelection(true);
    }

    protected virtual void ConfirmSelection(bool confirmed)
    {
        confirmation?.Deactivate();
        Hide(false);
        if (confirmed)
        {
            hasSelected = true;
            selectableGroup?.Select(index);
        }
    }

    public void SetUp(string txt = null) {
        if (txt != null)
        {
            text = txt;
            if(tmp) tmp.text = text;
        }
        hasSelected = false;
    }

    protected virtual void Hide(bool hide)
    {
        selectableGroup?.Hide(hide);
    }
}
