using System;
using System.Collections.Generic;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;
using UnityTimer;
using Void = EditorAttributes.Void;

[CreateAssetMenu(fileName = "InkUIIntegration_SO", menuName = "Game Module/Ink/Ink UI Integration")]
public class InkUIIntegrationSO : GameModule, IRegistrySO
{
    public bool CanSkipOrNext { get; private set; } = true;

    // key selectable
    public static readonly string DEFAULT_CHOICE_GROUP = "default";
    // key textPrinter
    public static readonly string DEFAULT_DIALOG_BOX = "default";
    //private static readonly string CHAPTER_TITLE = "CHAPTER_TITLE";
    private static readonly string CHARACTER_NAME = "CHARACTER_NAME";

    private static readonly string HIDDEN_CHARACTER_NAME = "???";
    // key sprite
    
    
    [Header("Config")]
    [SerializeField] private InkSystemSO inkSystem;
    [SerializeField] private string storyName;
    [SerializeField] private bool autoPlay;
    [SerializeField,ShowField(nameof(autoPlay))] private float autoPlayDelay = 3f;
    [SerializeField] private bool showHiddenChoice = true;
    
    [Header("Bucket")]
    [SerializeField] private SpriteBucket spriteBucket;
    
    
    [FoldoutGroup("Event Channels", nameof(inkTaskChannel), nameof(choiceEventChannel))]
    [SerializeField] private Void eventChannelGroup;
    // [HideInInspector,SerializeField] EventChannel<(string,Keeper)> varChangeChannel;
    [HideInInspector,SerializeField] InkTaskEventChannel inkTaskChannel;
    [HideInInspector, SerializeField] private IntEventChannelSO choiceEventChannel;

    [Header("Runtime")]

    // [RuntimeObject] private Button _nextButton;
    // [RuntimeObject] private Button _skipButton;
    
    [RuntimeObject] private readonly Dictionary<string,ISelectableGroup> _selectableGroups = new();
    [RuntimeObject] private readonly Dictionary<string,TextPrinterBase> _dialogBoxes = new();
    [RuntimeObject] private readonly Dictionary<string,SpriteController> _sprites = new();
    [RuntimeObject] private readonly Dictionary<string,Playable> _playables = new();
    
    [RuntimeObject] public string currentSceneName;
    [RuntimeObject] private string _currentLineBox = DEFAULT_DIALOG_BOX;
    
    #region General
    public InkUIIntegrationSO() {
        updateMode = UpdateMode.NONE;
    }
    
    public override void OnStart()
    {
        base.OnStart();
        CanSkipOrNext = true;
        inkSystem = GameRunner.GetSystem<InkSystemSO>().OrElse(null);
        if(currentSceneName=="") currentSceneName = inkSystem.GetLastSceneName();
    }

    private void OnEnable() {
        inkTaskChannel?.AddListener(HandleTask);
        choiceEventChannel?.AddListener(SelectChoice);
        InkSystemSO.InkTextAction += HandleText;
    }
    
    private void OnDisable() {
        inkTaskChannel?.RemoveListener(HandleTask);
        choiceEventChannel?.RemoveListener(SelectChoice);
        InkSystemSO.InkTextAction -= HandleText;
    }

    private void HandleText(Either<InkLine, InkChoice> either)
    {
        if(either.IsLeft) SetupLine(either.Left);
        else
        {
            SetupChoiceBox(either.Right);
        }
    }

    public void Clean()
    {
        // disable all persistent overhaul
        _selectableGroups[DEFAULT_CHOICE_GROUP].Reset();
        _dialogBoxes[DEFAULT_DIALOG_BOX].Clean();
    }
    #endregion
    
    #region Registry
    public void Register(Transform transform)
    {
        bool found = false;
        bool enabled = false;
        if (transform.TryGetComponent<ISelectableGroup>(out var selectableGroup))
        {
            if (_selectableGroups.TryAdd(selectableGroup.ChoiceGroupName, selectableGroup))
            {
                found = true;
                enabled = selectableGroup.EnableOnAwake;
            }
            else DebugUtil.Error("Duplicate gameObject " + selectableGroup.ChoiceGroupName, name);
        }
        
        if (transform.TryGetComponent<TextPrinterBase>(out var textPrinter))
        {
            if (textPrinter is InkTaskPrinter itp)
            {
                if(_dialogBoxes.TryAdd(itp.taskName, textPrinter)) found = true;
                else DebugUtil.Error("Duplicate gameObject " + textPrinter.printerName, name);
            } else if (_dialogBoxes.TryAdd(textPrinter.printerName, textPrinter))
            {
                found = true;
            } else DebugUtil.Error("Duplicate gameObject " + textPrinter.printerName, name);
        }
        
        if (transform.TryGetComponent<SpriteController>(out var spriteController))
        {
            if (_sprites.TryAdd(transform.name, spriteController))
            {
                found = true;
            } else DebugUtil.Error("Duplicate gameObject " + transform.name, name);
        }

        foreach(var playable in transform.GetComponents<Playable>())
        {
            if (_playables.TryAdd(playable.playbaleName, playable))
            {
                found = true;
                enabled = !playable.disableOnAwake;
            } else DebugUtil.Error("Duplicate gameObject " + playable.playbaleName, name);
        }
        
        transform.gameObject.SetActive(enabled);
        if(found) return;
        
        DebugUtil.Error("Selectable/TextPrinter/SpriteController/Playable is not found on gameObject " + transform.name, name);
    }
    
    public void Unregister(Transform transform)
    {
        if (transform.TryGetComponent<ISelectableGroup>(out var selectableGroup))
        {
            _selectableGroups.Remove(selectableGroup.ChoiceGroupName);
        }
        if (transform.TryGetComponent<TextPrinterBase>(out var textPrinter))
        {
            _dialogBoxes.Remove(textPrinter.printerName);
        }
        if (transform.TryGetComponent<SpriteController>(out var spriteController))
        {
            _sprites.Remove(transform.name);
        }
        if (transform.TryGetComponent<Playable>(out var playable))
        {
            _playables.Remove(playable.playbaleName);
        }
    }
    #endregion
    // no data stored, not in lifecycle, cross scene
    // UI buttons
    // animation, character expression, character staging (front back)
    // background image change
    
    // // UI: Start story, exit, choose, next line
    // // ink: trigger effect
    // // animation template: bg change, cross fade
    // // character sprite fade, appear: pre-marked location

    #region Line

    private TextPrinterBase _dialogBox;
    private bool _lineInterrupted = false;
    private void SetupLine(InkLine line)
    {
        
        string dialogBoxName;
        if (!string.IsNullOrEmpty(line.dialogBoxId))
        {
            _currentLineBox = line.dialogBoxId;
        }
        dialogBoxName = _currentLineBox;
        
        if(_dialogBox && dialogBoxName != _dialogBox.name) _dialogBox.Clean();
        
        string text = line.text;

        if(line.dialogue)
            SetupCharacterImage(line);
        
        _dialogBox = _dialogBoxes[dialogBoxName];
        _dialogBox.gameObject.SetActive(true);
        
        // interrupted
        _lineInterrupted = line.interrupted;
        _dialogBox.ReturnEarly = line.interrupted;
        
        if(_dialogBox is ChatBubbleQueue) _dialogBox.Print(text,AutoPlay, line.dialogBoxSubId);
        else _dialogBox.Print(text,AutoPlay);
        
        _skipped.Reset();
       
        SetupSpeaker(line);
        

    }
    
    private void SetupSpeaker(InkLine line)
    {
        if (line.dialogue && (string.IsNullOrEmpty(line.dialogBoxId) || line.dialogBoxId == DEFAULT_DIALOG_BOX))
        {
            string characterName = line.hide? HIDDEN_CHARACTER_NAME : TranslationUtil.Translate(line.character);
            _dialogBoxes[CHARACTER_NAME].gameObject.SetActive(true);
            _dialogBoxes[CHARACTER_NAME].Print(characterName);
        }
        else
        {
            _dialogBoxes[CHARACTER_NAME].gameObject.SetActive(false);
        }
        
    }
    
    Timer _autoPlayTimer;
    private void AutoPlay() // only in line
    {
        if (_lineInterrupted)
        {
            inkSystem.Next();
        }
        if (autoPlay)
        {
            if (_autoPlayTimer == null)
            {
                _autoPlayTimer = new CountdownTimer(autoPlayDelay);
                _autoPlayTimer.OnTimerStop += () => inkSystem.Next();
            }
            
            _autoPlayTimer.Reset();
            _autoPlayTimer.Start();
        }
    }

    private readonly Flip _skipped = new();
    public void SkipOrNext()
    {
        if (_dialogBox)
        {
            if( !_skipped && !_dialogBox.Done) _dialogBox.Skip();
            else
            {
                _autoPlayTimer?.Stop();
                if(_dialogBox.hideWhenNotUsed) _dialogBox.gameObject.SetActive(false);
                inkSystem.Next();
            }
        }
    }

    private string _lastPortraitPosition;
    private void SetupCharacterImage(InkLine line)
    {
        if(line.portraitId.IsEmpty() ) return;
        if (_lastPortraitPosition != line.portraitPosition)
        {
            _sprites[_lastPortraitPosition].Clear();
        }
        var sc = _sprites[line.portraitPosition];
        sc.SwapImage(spriteBucket.Get(line.portraitId).Get());
        _lastPortraitPosition = line.portraitPosition;
    }
    
    #endregion
    #region Choice
    private string _choiceGroupName;
    private void SetupChoiceBox(InkChoice choiceInfo)
    {
        if(_dialogBox)_dialogBox.Clean();
        Debug.Log($"Choice {choiceInfo.groupId}");
        _choiceGroupName = choiceInfo.groupId.IsEmpty()? DEFAULT_CHOICE_GROUP : choiceInfo.groupId;
        _selectableGroups[_choiceGroupName].Activate(choiceInfo, showHiddenChoice);
        
        CanSkipOrNext = false;
    }
    
    private void ResetSelectableGroup(string groupName) {
        if(!_selectableGroups.TryGetValue(groupName, out var selectableGroup)) return;
        selectableGroup.Reset();
    }
    
    public void SelectChoice(int index) {
        Debug.Log($"Select choice {index}");
        ResetSelectableGroup(_choiceGroupName);
        _choiceGroupName="";
        CanSkipOrNext = true;
        inkSystem.Next(index);
    }
    
    #endregion
    
    #region Task

    private void HandleTask((string, string, Action<string>) task)
    {
        string taskName = task.Item1;
        string parameter = task.Item2;
        Action<string> callback = task.Item3;
        
        var op = _dialogBoxes.Get(taskName)
            .Do(db =>db.Print(parameter, () => { callback?.Invoke(taskName); }));
        if(op.HasValue) return;
        
        if (taskName == InkConstants.TASK_PLAY_CG)
        {
            var arr = parameter.Split('_', 2);
            if(arr.Length == 1) _playables[arr[0]]?.Play(callback);
            else _playables[arr[0]]?.Play(callback, arr[1]);
            return;
        } 
        if (taskName == InkConstants.TASK_HIDE_CG)
        {
            _playables[parameter]?.End();
            callback(taskName);
            return;
        } 
    }

    
    #endregion
}