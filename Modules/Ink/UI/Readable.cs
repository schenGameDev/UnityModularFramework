using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// display text somewhere else after clicked
/// </summary>
[RequireComponent(typeof(Button))]
public class Readable : TranslationText
{
    private static readonly int STOP = Animator.StringToHash("Stop");
    public const string DEFAULT_PRINTER = "info";
    
    [SerializeField,TextArea] private string text;
    [SerializeField] public string printerName;
    [SerializeField] private TextPrinter printer;
    [SerializeField] private GameObject detail;
    [SerializeField] private Animator indicator;
    
    private bool _isShow;
    
    protected override void Start()
    {
        if (!printer)
        {
            printer = TextPrinter.INSTANCES.Get(string.IsNullOrEmpty(printerName)? DEFAULT_PRINTER : printerName).OrElse(null); // other scene printer
        } else
        {
            printer.gameObject.SetActive(false); // local scene printer
        }
    } // stop parent Start
    
    public void Read()
    {
        if(!printer && !detail) return;
        if(_isShow) return;
        Print();
        if (detail)
        {
            detail.SetActive(true);
        }
        _isShow = true;
        if(indicator) indicator.SetBool(STOP, true);
    }
    
    private void Print() => printer?.Print(text, PrintDone);

    private void Skip()
    {
        if(!printer || !_isShow) return;
        if (printer.Done)
        {
            printer.Clean();
        }
        else printer.Skip();
    }

    private readonly Flip _waitPlayerConfirm = new();
    private void PrintDone()
    {
        if(!_isShow) return;

        if (!_waitPlayerConfirm)
        {
            return;
        }
        printer.Clean();
        
        if (detail)
        {
            detail.SetActive(false);
        }
        _isShow = false;
    }

    public void OnHover()
    {
        //GetComponent<Image>()?.;
    }
#if UNITY_EDITOR
    protected override string GetDraftText() => text;
#endif
}