using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Commons;
using ModularFramework.Utility;
using ModularFramework.Utility.Translation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "InkUIIntegration_SO", menuName = "Game Module/Ink UI Integration")]
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
    [RuntimeObject] private readonly Dictionary<string,TextPrinter> _dialogBoxes = new();
    [RuntimeObject] private readonly Dictionary<string,SpriteController> _sprites = new();
    
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
        var selectable = transform.GetComponent<Selectable>();
        if (selectable)
        {
            _selectables.GetOrCreateDefault(selectable.choiceGroupName).Add(selectable);
            return;
        }
        
        var textPrinter = transform.GetComponent<TextPrinter>();
        if (textPrinter)
        {
            if (_dialogBoxes.TryAdd(transform.name, textPrinter))
            {
                return;
            }
            DebugUtil.Error("Duplicate gameObject " + transform.name, name);
        }
        
        var spriteController = transform.GetComponent<SpriteController>();
        if (spriteController)
        {
            if (_sprites.TryAdd(transform.name, spriteController))
            {
                return;
            }
            DebugUtil.Error("Duplicate gameObject " + transform.name, name);
        }
        
        DebugUtil.Error("Selectable/TextPrinter/SpriteController is not found on gameObject " + transform.name, name);
    }
    
    public void Unregister(Transform transform)
    {
        var selectable = transform.GetComponent<Selectable>();
        if (selectable)
        {
            _selectables[selectable.choiceGroupName]?.Remove(selectable);
            return;
        }
        var textPrinter = transform.GetComponent<TextPrinter>();
        if (textPrinter)
        {
            _dialogBoxes.Remove(transform.name);
            return;
        }
        var spriteController = transform.GetComponent<SpriteController>();
        if (spriteController)
        {
            _sprites.Remove(transform.name);
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

    private TextPrinter _dialogBox;
    private void SetupLine(InkLine line)
    {
        string characterName = line.hide? HIDDEN_CHARACTER_NAME : string.Join(", ",line.characters.Select(TranslationUtil.Translate));
        var dialogBoxName = string.IsNullOrEmpty(line.dialogBoxId)? DEFAULT_DIALOG_BOX : line.dialogBoxId;
        
        if(_dialogBox && dialogBoxName != _dialogBox.name) _dialogBox.Clean();
        
        string text = line.text;
        string subtext = "";
#if UNITY_EDITOR
        subtext = $"(Character unknown because {line.subText})"; // true condition expression
#endif
        
        _dialogBoxes[CHARACTER_NAME].Print(characterName);
        _dialogBox = _dialogBoxes[dialogBoxName];
        _dialogBox.Print($"{text} <color=\"red\">{subtext}</color>");
        _skipped.Reset();

        SetupCharacterImage(line);

    }

    private readonly Flip _skipped = new();
    public void SkipOrNext()
    {
        if (_dialogBox)
        {
            if( !_skipped && !_dialogBox.Done) _dialogBox.Skip();
            else
            {
                inkSystem.Next();
            }
        }
    }

    private string _lastPortraitPosition;
    private void SetupCharacterImage(InkLine line)
    {
        if (_lastPortraitPosition != line.portraitPosition)
        {
            _sprites[_lastPortraitPosition].Clear();
        }
        _sprites[line.portraitPosition].SwapImage(spriteBucket.Get(line.portraitId).Get());
        _lastPortraitPosition = line.portraitPosition;
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

        if (taskName == EnvironmentConstants.TASK_CHANGE_SCENE)
        {
            SceneLoader.Instance.LoadScene(parameter,null,callback);
        } else if (taskName == EnvironmentConstants.TASK_PLAY_SOUND)
        {
            sfxChannel.Raise(parameter);
            callback?.Invoke(taskName);
        } else if (taskName == EnvironmentConstants.TASK_PLAY_BGM)
        {
            bgmChannel.Raise(parameter);
            callback?.Invoke(taskName);
        }
        
        // character emotion change (fade inout)
        // character change (fade inout)
        // show image
        // play animation
        // sprites (name, img name)?
    }
    
    #endregion
}