using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.UI;
using UnityTimer;

[CreateAssetMenu(fileName = "InkUIIntegration_SO", menuName = "Game Module/Ink/Ink UI Integration")]
public class InkUIIntegrationSO : GameModule, IRegistrySO {
    // key selectable
    private static readonly string DEFAULT_CHOICE_GROUP = "DEFAULT";
    // key textPrinter
    private static readonly string DEFAULT_DIALOG_BOX = "DEFAULT";
    private static readonly string CHAPTER_TITLE = "CHAPTER_TITLE";
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
    
    
    [FoldoutGroup("Event Channels", nameof(inkTextChannel), nameof(inkTaskChannel), nameof(choiceEventChannel), nameof(sfxChannel), nameof(bgmChannel))]
    [SerializeField] private EditorAttributes.Void eventChannelGroup;
    // [HideInInspector,SerializeField] EventChannel<(string,Keeper)> varChangeChannel;
    [HideInInspector,SerializeField] EventChannel<Either<InkLine,InkChoice>> inkTextChannel;
    [HideInInspector,SerializeField] EventChannel<(string,string,Action<string>)> inkTaskChannel;
    [HideInInspector, SerializeField] private EventChannel<int> choiceEventChannel;
    [HideInInspector, SerializeField] private EventChannel<string> sfxChannel;
    [HideInInspector, SerializeField] private EventChannel<string> bgmChannel;
    [Header("Runtime")]

    [RuntimeObject] private Button _nextButton;
    [RuntimeObject] private Button _skipButton;
    
    [RuntimeObject] private readonly Dictionary<string,List<Selectable>> _selectables = new();
    [RuntimeObject] private readonly Dictionary<string,TextPrinterBase> _dialogBoxes = new();
    [RuntimeObject] private readonly Dictionary<string,SpriteController> _sprites = new();
    [RuntimeObject] private readonly Dictionary<string,Playable> _playables = new();
    
    [RuntimeObject] private OnetimeFlip _storyStarted;
    
    #region General
    public InkUIIntegrationSO() {
        RefKeywords = new[]{"NEXT_BUTTON","SKIP_BUTTON",};
        updateMode = UpdateMode.NONE;
    }
    
    public override void OnAwake(Dictionary<string, string> flags, Dictionary<string, GameObject> references)
    {
        base.OnAwake(flags, references);
        _nextButton = references["NEXT_BUTTON"].GetComponent<Button>();
        _skipButton = references["SKIP_BUTTON"].GetComponent<Button>();
    }

    public override void OnStart()
    {
        base.OnStart();
        if (!_storyStarted)
        {
            SaveUtil.GetValue(InkConstants.KEY_CURRENT_STORY).Do(sn => storyName = sn);
            inkSystem.StartStory(storyName);
        }
    }

    private void OnEnable() {
        inkTextChannel?.AddListener(HandleText);
        inkTaskChannel?.AddListener(HandleTask);
        choiceEventChannel?.AddListener(SelectChoice);
    }
    
    private void OnDisable() {
        inkTextChannel?.RemoveListener(HandleText);
        inkTaskChannel?.RemoveListener(HandleTask);
        choiceEventChannel?.RemoveListener(SelectChoice);
    }

    private void HandleText(Either<InkLine, InkChoice> either)
    {
        if(either.IsLeft) SetupLine(either.Left);
        else
        {
            SetupChoiceBox(either.Right);
        }
    }
    #endregion
    
    #region Registry
    public void Register(Transform transform)
    {
        bool found = false;
        if (transform.TryGetComponent<Selectable>(out var selectable))
        {
            _selectables.GetOrCreateDefault(selectable.choiceGroupName).Add(selectable);
            found = true;
        }
        
        if (transform.TryGetComponent<TextPrinterBase>(out var textPrinter))
        {
            if (textPrinter is InkTaskPrinter itp)
            {
                if(_dialogBoxes.TryAdd(itp.taskName, textPrinter)) found = true;
            } else if (_dialogBoxes.TryAdd(transform.name, textPrinter))
            {
                found = true;
            }
            DebugUtil.Error("Duplicate gameObject " + transform.name, name);
        }
        
        if (transform.TryGetComponent<SpriteController>(out var spriteController))
        {
            if (_sprites.TryAdd(transform.name, spriteController))
            {
                found = true;
            }
            DebugUtil.Error("Duplicate gameObject " + transform.name, name);
        }

        if (transform.TryGetComponent<Playable>(out var playable))
        {
            if (_playables.TryAdd(transform.name, playable))
            {
                found = true;
            }
            DebugUtil.Error("Duplicate gameObject " + transform.name, name);
        }
        
        if(found) return;
        
        DebugUtil.Error("Selectable/TextPrinter/SpriteController/Playable is not found on gameObject " + transform.name, name);
    }
    
    public void Unregister(Transform transform)
    {
        if (transform.TryGetComponent<Selectable>(out var selectable))
        {
            _selectables[selectable.choiceGroupName]?.Remove(selectable);
        }
        if (transform.TryGetComponent<TextPrinterBase>(out var textPrinter))
        {
            _dialogBoxes.Remove(transform.name);
        }
        if (transform.TryGetComponent<SpriteController>(out var spriteController))
        {
            _sprites.Remove(transform.name);
        }
        if (transform.TryGetComponent<Playable>(out var playable))
        {
            _playables.Remove(transform.name);
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
    private void SetupLine(InkLine line)
    {
        
        var dialogBoxName = string.IsNullOrEmpty(line.dialogBoxId)? DEFAULT_DIALOG_BOX : line.dialogBoxId;
        
        if(_dialogBox && dialogBoxName != _dialogBox.name) _dialogBox.Clean();
        
        string text = line.text;
        string subtext = "";
#if UNITY_EDITOR
        subtext = $"(Character unknown because {line.subText})"; // true condition expression
#endif
        var sc = SetupCharacterImage(line);
        
        _dialogBox = _dialogBoxes[dialogBoxName];
        string t = $"{text} <color=\"red\">{subtext}</color>";
        
        if(_dialogBox is ChatBubbleQueue) _dialogBox.Print(t,AutoPlay, isSpeakerOnLeftSide(sc.transform)? "1" : "0");
        else _dialogBox.Print(t,AutoPlay);
        
        _skipped.Reset();
       
        SetupSpeaker(line);
        

    }

    private bool isSpeakerOnLeftSide(Transform transform)
    {
        var screenPos = Camera.main.WorldToScreenPoint(transform.position);
        return screenPos.x < Screen.width / 2;
    }

    private void SetupSpeaker(InkLine line)
    {
        if (line.dialogue)
        {
            string characterName = line.hide? HIDDEN_CHARACTER_NAME : string.Join(", ",line.characters.Select(TranslationUtil.Translate));
            _dialogBoxes[CHARACTER_NAME].gameObject.SetActive(true);
            _dialogBoxes[CHARACTER_NAME].Print(characterName);
        }
        else
        {
            _dialogBoxes[CHARACTER_NAME].gameObject.SetActive(false);
        }
        
    }
    
    Timer _autoPlayTimer;
    private void AutoPlay()
    {
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
                inkSystem.Next();
            }
        }
    }

    private string _lastPortraitPosition;
    private SpriteController SetupCharacterImage(InkLine line)
    {
        if (_lastPortraitPosition != line.portraitPosition)
        {
            _sprites[_lastPortraitPosition].Clear();
        }
        var sc = _sprites[line.portraitPosition];
        sc.SwapImage(spriteBucket.Get(line.portraitId).Get());
        _lastPortraitPosition = line.portraitPosition;
        return sc;
    }
    
    #endregion
    #region Choice
    private string _choiceGroupName;
    private void SetupChoiceBox(InkChoice choiceInfo)
    {
        if(_dialogBox)_dialogBox.Clean();
        
        _choiceGroupName = choiceInfo.groupId.IsEmpty()? DEFAULT_CHOICE_GROUP : choiceInfo.groupId;
        var buttons = _selectables[_choiceGroupName];
        int i = 0;
        List<InkLine> choices = choiceInfo.choices;
        
        foreach(var choice in choices) {
            string text = choice.text;
            string subtext = "";
#if UNITY_EDITOR
            subtext = $"({choice.subText})";
#endif

            if (!showHiddenChoice && choice.hide)
            {
                continue;
            }

            var b = buttons.First(b => b.index == i);
            b.Activate(choice.hide? $"<color=\"grey\">{text}</color> <color=\"red\">{subtext}</color>" : text);
            i++;
        }
        _nextButton.interactable = false;
        _skipButton.interactable = false;
    }
    
    private void ActivateAllSelectables(string groupName, bool isActivate) {
        if(!_selectables.TryGetValue(groupName, out var selectable)) return;
    
        foreach (var s in selectable) {
            if(isActivate) s.Activate();
            else s.Deactivate();
        }
    }
    
    public void SelectChoice(int index) {
        inkSystem.Next(index);
        ActivateAllSelectables(_choiceGroupName, false);
        _choiceGroupName="";
        _nextButton.interactable = true;
        _skipButton.interactable = true;
    }
    
    #endregion
    
    #region Task

    private void HandleTask((string, string, Action<string>) task)
    {
        string taskName = task.Item1;
        string parameter = task.Item2;
        Action<string> callback = task.Item3;
        
        _dialogBoxes[taskName]?.Print(parameter, () => callback(taskName));
        
        if (taskName == InkConstants.TASK_CHANGE_SCENE)
        {
            GameBuilder.Instance.LoadScene(parameter,null,()=>SceneLoaded(parameter,callback));
            // _dialogBoxes[CHAPTER_TITLE].gameObject.SetActive(true);
            // _dialogBoxes[CHAPTER_TITLE].Print(TranslationUtil.Translate(parameter));
        } else if (taskName == InkConstants.TASK_PLAY_SOUND)
        {
            sfxChannel.Raise(parameter);
            callback?.Invoke(taskName);
        } else if (taskName == InkConstants.TASK_PLAY_BGM)
        {
            bgmChannel.Raise(parameter);
            callback?.Invoke(taskName);
        } else if (taskName == InkConstants.TASK_PLAY_CG)
        {
            _playables[taskName]?.Play(callback);
            inkTaskChannel.Raise((taskName, parameter, null));
        } else if (taskName == InkConstants.TASK_ADD_NOTE)
        {
            if (!inkSystem.notes.Contains(parameter))
            {
                inkSystem.notes.Add(parameter);
                inkTaskChannel.Raise((taskName, parameter, null));
            }
            callback?.Invoke(taskName);
        }
        else
        {
            throw new Exception("Unknown task type: " + taskName);
        }
    }

    private void SceneLoaded(string sceneName,  Action<string> callback)
    {
        inkTaskChannel.Raise((InkConstants.TASK_CHANGE_SCENE, sceneName, null));
        callback?.Invoke(InkConstants.TASK_CHANGE_SCENE);
    }
    
    public List<string> ShowAllNotes() => inkSystem.notes;
    
    #endregion
}