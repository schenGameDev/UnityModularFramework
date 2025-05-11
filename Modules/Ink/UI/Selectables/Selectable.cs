using EditorAttributes;
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

    protected TextMeshProUGUI TMP;
    private SelectableGroup _selectableGroup;
        
    
    protected virtual void Awake()
    {
        TMP = GetComponentInChildren<TextMeshProUGUI>(true);
        _selectableGroup = GetComponentInParent<SelectableGroup>(true);
        if (!_selectableGroup)
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
            _selectableGroup?.Select(index);
        }
    }

    public void SetUp(string txt = null) {
        if (txt != null)
        {
            text = txt;
            if(TMP) TMP.text = text;
        }
        hasSelected = false;
    }

    protected virtual void Hide(bool hide)
    {
        _selectableGroup?.Hide(hide);
    }
}
