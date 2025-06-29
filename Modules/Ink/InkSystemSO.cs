using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using Ink;
using Ink.Runtime;
using ModularFramework;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;
using UnityTimer;
using ValueType = ModularFramework.Commons.ValueType;
using Void = EditorAttributes.Void;

/// <summary>
/// load and save story, send and receive variables.
/// read var from story into memory -> any update go in story -> save story state -> clean all memory
/// </summary>
[CreateAssetMenu(fileName = "InkSystem_SO", menuName = "Game Module/Ink/Ink System")]
public class InkSystemSO : GameSystem
{
    private const string IN_TEXT_CODE_PATTERN = "[{][^{}]+[}]";
    public const string NARRATIVE_TEXT = "NARRATIVE";
    
    [Header("Config")]
    [SerializeField] private InkStoryBucket stories;
    [SerializeField] private InkTagDefBucket[] tagDefBuckets;
    [SerializeField] private Bucket varNameBucket;
    [SerializeField] private Bucket codeReplaceBucket;
    [SerializeField] private bool saveLog;

    [FoldoutGroup("Event Channels", nameof(varChangeChannel), nameof(inkTaskChannel))]
    [SerializeField] private Void eventChannelGroup;
    [HideInInspector,SerializeField] InkVarEventChannel varChangeChannel;
    [HideInInspector,SerializeField] InkTaskEventChannel inkTaskChannel;

    public static Action<Either<InkLine, InkChoice>> InkTextAction;

    [Header("Runtime")]
    [Rename("Current Story"),ReadOnly,SerializeField,RuntimeObject] string currentStoryName;
    [ReadOnly,SerializeField,RuntimeObject] private string currentChapter;
    [RuntimeObject] private readonly Dictionary<string, string> _characterDialogBox = new();

#if UNITY_EDITOR
    [ReadOnly,SerializeField,SerializedDictionary,RuntimeObject] private SerializedDictionary<string,string> stats;
#endif
    // variable first save to story, then populate keeper through delegate, then alert unity through event channel
    [RuntimeObject] private readonly Dictionary<string,Keeper> _keeperDict = new();
    [RuntimeObject(cleaner:nameof(ClearStory))] private Story _currentStory;
    [SerializeField,ReadOnly] InkStage stage;
    [RuntimeObject] InkLine _currentLine;
    [RuntimeObject] InkChoice _currentChoice;

    public override void OnDestroy() {
        stage = InkStage.END;
    }

    #region Story
    public void StartStory(string storyName)
    {
        if(_currentStory != null) ClearStory();

        stage = InkStage.READY;
        _currentStory = new Story(stories.Get(storyName).Get().text);

#if UNITY_EDITOR
        currentStoryName = storyName;
#endif
        LoadStory(storyName,_currentStory);

        _currentStory.ObserveVariables(new List<string>(_keeperDict.Keys),
            (varName, newValue) => {
                if(newValue == _keeperDict[varName]) return;
                PutKeeper(varName, newValue);
                Debug.Log("Ink Var Changed: " + varName);
                varChangeChannel?.Raise((varName, PutKeeper(varName, newValue)));
            });

        _currentStory.onError += (msg, type) => {
            if( type == ErrorType.Warning )
                DebugUtil.Warn(msg);
            else
                DebugUtil.Error(msg);
        };
    }

    private void ClearStory() {
        _currentStory?.RemoveVariableObserver();
        _currentStory = null;
        _firstLine = true;
    }

    /// <returns> true until story meets a choice or end</returns>
    public bool CanContinue() {
        if(_currentStory == null) {
            DebugUtil.Error("No story loaded.");
            return false;
        }
        if(_currentStory.canContinue) {
            return true;
        }
        return false;
    }

    [RuntimeObject] int _lastChoiceIndex = -1;
    [RuntimeObject] bool _firstLine = true;
    public InkStage Next(int choiceIndex = -1) { // next button clickable only at READY state
        if(TaskRunning()) {
            if(stage == InkStage.WAIT_CHOICE) _lastChoiceIndex = choiceIndex;
            _playNextAfterTaskCompleted = true;
            return InkStage.WAIT_TASK;
        }
        _playNextAfterTaskCompleted = false;
        _lastChoiceIndex = -1;
        // Debug.Log("ink");
        if(stage == InkStage.READY) {
            _currentChoice = null;
            _currentLine = null;
            // check if story end or choice ahead
            if(!CanContinue()) {
                _currentChoice = NextChoice();
                if(_currentChoice == null || _currentChoice.choices.Count == 0) {
                    stage = InkStage.END;
                    GameBuilder.Instance.LoadScene("Menu");
                    return stage;
                } 
                InkTextAction.Invoke(Either<InkLine,InkChoice>.FromRight(_currentChoice));
                stage = InkStage.WAIT_CHOICE;
            } else {
                string text = ReplaceCode(_currentStory.Continue()).Trim();
                if (text == InkConstants.VAR_SAVE)
                {
                    if (!_firstLine)
                    {
                        SaveStory(currentStoryName, _currentStory);
                        GameRunner.GetSystem<NoteSystemSO>().Do(sys => sys.SaveNotes());
                        SaveUtil.FlushToNextAvailableSlot();
                    }
                    _firstLine = false;
                    return Next(choiceIndex);
                }
                _firstLine = false;
                if (text.IsBlank())
                {
                    return Next(choiceIndex);
                }
                
                if (text == InkConstants.VAR_PAUSE)
                {
                    _playNextAfterTaskCompleted = true;
                    return InkStage.WAIT_TASK;
                }
                
                if (text == InkConstants.VAR_WAIT)
                {
                    return InkStage.WAIT_TASK;
                }
                
                
                List<InkTag> tags = _currentStory.currentTags==null? new() : _currentStory.currentTags.Select(t=>InkTag.Of(t,tagDefBuckets,Get)).ToList();
                _currentLine = new InkLine(text,tags);
                UpdateDialogueBox(_currentLine);
                
                SaveLog(text);
                InkTextAction.Invoke(Either<InkLine,InkChoice>.FromLeft(_currentLine));
                stage = InkStage.READY;
            }
            return stage;
        }

        if(stage == InkStage.WAIT_CHOICE) {
            if(choiceIndex<0 || choiceIndex>=_currentChoice.choices.Count) {
                throw new ArgumentOutOfRangeException("");
            }
            _currentChoice = null;
            _currentStory.ChooseChoiceIndex(choiceIndex);
            stage = InkStage.READY;
            return Next();
        }

        throw new Exception("Wrong place to be");
    }

    private void UpdateDialogueBox(InkLine line)
    {
        string character = string.IsNullOrEmpty(line.character)? NARRATIVE_TEXT : line.character;
        if (string.IsNullOrEmpty(line.dialogBoxId))
        {
            line.dialogBoxId = _characterDialogBox.Get(character).OrElse(InkUIIntegrationSO.DEFAULT_DIALOG_BOX);
        }
        else
        {
            _characterDialogBox[character] = line.dialogBoxId;
        }
    }


    private InkChoice NextChoice() {
        if(_currentStory.currentChoices == null) return null;

        List<InkLine> choices = new();

        for(int i=0;i<_currentStory.currentChoices.Count;i++) {
            var choice = _currentStory.currentChoices[i];
            string text = ReplaceCode(choice.text);
            List<InkTag> tags =choice.tags==null? new() : choice.tags.Select(t=>InkTag.Of(t,tagDefBuckets,Get)).ToList();
            choices.Add(new(text,tags, true));
        }
        return new InkChoice(choices,ExplainCondition);
    }
    
    private string ReplaceCode(string text)
    {
        if(!codeReplaceBucket || text == null) return text;
        
        try
        {
            var matches = Regex.Matches(text, IN_TEXT_CODE_PATTERN, RegexOptions.None,
                TimeSpan.FromSeconds(0.2f));
            if(matches.Count == 0) return text;
            
            StringBuilder sb = new();
            int i = 0;
            foreach (Match match in matches)
            {
                sb.Append(text.SubstringBetween(i, match.Index));
                i = match.Index + match.Length;
                sb.Append(codeReplaceBucket.Get(match.Value.Substring(1, match.Length - 1)));
            }
            return sb.ToString();
        }
        catch (RegexMatchTimeoutException) {
            // Do Nothing: Assume that timeout represents no match.
        }
        
        return text;
    }

    private void LoadStory(string storyName, Story story) {
        SaveUtil.GetState(storyName)
            .Do(states => story.state.LoadJson(states))
            .OrElseDo(story.ResetState);
        InjectVariables(story);
        SaveUtil.GetState(InkConstants.KEY_CURRENT_SCENE_HISTORY)
            .Do(states =>
            {
                var history = JsonUtility.FromJson<List<(string, string, bool)>>(states);
                if(history.IsEmpty()) return;
                _taskHistory = history;
                foreach (var th in _taskHistory)
                {
                    RunTask(th.Item1, th.Item2, th.Item3);
                } 
            });
        
        story.variablesState.ForEach(varName => PutKeeper(varName,story.variablesState[varName]));
        story.BindExternalFunction(InkConstants.INK_FUNCTION_DO_TASK, 
            (string task, string parameter, bool isBlocking) => DoTask(task, parameter, isBlocking),
            false);
    }

    public void SaveStory(string storyName, Story story) {
        string saveJson =story.state.ToJson();
        SaveUtil.SaveState(storyName, saveJson);
        SaveUtil.SaveState(InkConstants.KEY_CURRENT_STORY, storyName);
        SaveUtil.SaveState(InkConstants.KEY_CURRENT_SCENE_HISTORY,  JsonUtility.ToJson(_taskHistory));
        SaveVariables();
    }

    public string GetLastSceneName()
    {
        return _keeperDict.Get(InkConstants.VAR_SCENE).OrElse( Keeper.Of("UNDEFINED"));
    }
#endregion
#region Task
    [RuntimeObject] private List<(string, string, bool)> _taskHistory = new(); // clear when scen

    [RuntimeObject] int _tasksRunning = 0;
    [RuntimeObject] readonly List<(string,string,bool)> _taskBuffer = new();
    [RuntimeObject] bool _taskBlocked = false;
    [RuntimeObject] bool _playNextAfterTaskCompleted = false;

    private void DoTask(string taskHandler, string parameter, bool isBlocking) {
        // wait till all tasks finish, then proceed to next
        if(_taskBlocked) {
            _taskBuffer.Add((taskHandler, parameter, isBlocking));
            return;
        }
        RunTask(taskHandler, parameter, isBlocking);
        AuditHistory(taskHandler, parameter, isBlocking);
    }

    private void RunTask(string taskHandler, string parameter, bool isBlocking)
    {
        _tasksRunning++;
        
        RaiseTask(taskHandler, parameter);
        if(isBlocking) {
            _taskBlocked = true;
        }
    }

    private void RaiseTask(string taskHandler, string parameter)
    {
        Debug.Log($"Task {taskHandler}:{parameter} Started");
        if (taskHandler == InkConstants.TASK_CHANGE_SCENE)
        {
            GameBuilder.Instance.LoadScene(parameter,()=>SceneLoaded(parameter,TaskComplete));
            return;
        }
        
        if (taskHandler == InkConstants.TASK_HANG)
        {
            Timer timer = new CountdownTimer(float.Parse(parameter));
            timer.OnTimerStop += () =>
            {
                TaskComplete(InkConstants.TASK_HANG);
                _playNextAfterTaskCompleted = true;
                timer.Dispose();
            };
            timer.Start();
            return;
        }
        // immediate
        if (taskHandler == InkConstants.TASK_PLAY_SOUND)
        {
            GameRunner.Instance?.GetModule<SoundManagerSO>().Do(sys => sys.PlaySound(parameter));
            TaskComplete(taskHandler);
            return;
        }
        if (taskHandler == InkConstants.TASK_PLAY_BGM)
        {
            GameRunner.GetSystem<MusicSystemSO>().Do(sys => sys.PlayTrack(parameter));
            TaskComplete(taskHandler);
            return;
        }
        if (taskHandler == InkConstants.TASK_ADD_NOTE)
        {
            GameRunner.GetSystem<NoteSystemSO>().Do(sys => sys.AddNote(parameter));
            TaskComplete(taskHandler);
            return;
        }
        if (taskHandler == InkConstants.TASK_ADD_QUEST || 
            taskHandler == InkConstants.TASK_DROP_QUEST ||
            taskHandler == InkConstants.TASK_COMPLETE_QUEST)
        {
            GameRunner.GetSystem<QuestSystemSO>().Do(sys =>
            {
                Quest.QuestStage s = taskHandler switch
                {
                    InkConstants.TASK_DROP_QUEST => Quest.QuestStage.FAILED,
                    InkConstants.TASK_COMPLETE_QUEST => Quest.QuestStage.COMPLETED,
                    _ => Quest.QuestStage.ACTIVE
                };
                sys.UpdateQuestStage(parameter, s);
            });
            TaskComplete(taskHandler);
            return;
        }

        Action<string> callback = TaskComplete;
        if (taskHandler == InkConstants.TASK_NOTIFICATION)
        {
            callback(taskHandler);
            callback = null;
        }

        inkTaskChannel.Raise((taskHandler, parameter, callback));
    }

    private void AuditHistory(string taskHandler, string parameter, bool isBlocking)
    {
        if (taskHandler == InkConstants.TASK_CHANGE_SCENE)
        {
            _taskHistory.Clear();
        }
        // ignore instant task
        if (taskHandler is 
                InkConstants.TASK_HANG or InkConstants.TASK_PLAY_SOUND or InkConstants.TASK_ADD_NOTE or 
                InkConstants.TASK_ADD_QUEST or InkConstants.TASK_DROP_QUEST or InkConstants.TASK_COMPLETE_QUEST or 
                InkConstants.TASK_NOTIFICATION)
        {
            return;
        }
        // cancel prev task
        if (taskHandler is InkConstants.TASK_HIDE_CG)
        {
            if (_taskHistory.RemoveWhere(th => th.Item1 == taskHandler && th.Item2 == parameter))
            {
                return;
            }
        }
        // overwrite prev same task
        if (taskHandler is InkConstants.TASK_PLAY_BGM or InkConstants.TASK_TITLE)
        {
            _taskHistory.RemoveWhere(th => th.Item1 == taskHandler);
        }

        if (isBlocking)
        {
            _taskHistory.RemoveWhere(th => th.Item3);
        }
        // extend prev same task
        if (taskHandler is InkConstants.TASK_PLAY_CG) // slideShower
        {
            string prefix = parameter + "_";
            string origin = parameter;
            int count = 0;
            _taskHistory.RemoveWhere(th =>
            {
                if (th.Item1 == taskHandler)
                {
                    if (th.Item2.StartsWith(prefix))
                    {
                        count = int.Parse(th.Item2[prefix.Length..]) + 1;
                        return true;
                    }

                    if (th.Item2 == origin)
                    {
                        count++;
                        return true;
                    }
                }
                return false;
            });
            if (count > 0)
            {
                parameter = prefix + count;
            }
        }

        _taskHistory.Add((taskHandler, parameter, isBlocking));
    }

    [Button]
    private void PrintHistory()
    {
        
        _taskHistory.ForEach(th =>
        {
            string blockingMsg = th.Item3 ? ": Blocking" : "";
            Debug.Log($"{th.Item1} - {th.Item2}{blockingMsg}");
        });
    }

    private void SceneLoaded(string sceneName,  Action<string> callback)
    {
        inkTaskChannel.Raise((InkConstants.TASK_CHANGE_SCENE, sceneName, null));
        callback?.Invoke(InkConstants.TASK_CHANGE_SCENE);
    }
    

    public void TaskComplete(string taskName) {
        Debug.Log($"Task {taskName} is completed");
        if (--_tasksRunning > 0) return;
        _taskBlocked = false;
        
        if(!_taskBuffer.IsEmpty()) {
            var task = _taskBuffer.RemoveAtAndReturn(0);
            RunTask(task.Item1, task.Item2, task.Item3);
            return;
        }

        if (_playNextAfterTaskCompleted)
        {
            Next(_lastChoiceIndex);
        }
    }

    public bool TaskRunning() => _tasksRunning > 0;

#endregion

#region Variables
    private void InjectVariables(Story story) {
        if(_keeperDict.IsEmpty()) {
            // first story, inject from save
            var dict = SaveUtil.GetAllValues();
            if(dict != null) {
                dict.ForEach((varName, value) => {
                    bool neededInStory = story.variablesState.GlobalVariableExistsWithName(varName);

                    if(value.type == ValueType.Bool) {
                        if(neededInStory) Put(varName, (bool)value);
                        else PutKeeper(varName, (bool)value);
                    } else if(value.type == ValueType.Int) {
                        if(neededInStory) Put(varName, (int)value);
                        else PutKeeper(varName, (int)value);
                    } else if(value.type == ValueType.Float) {
                        if(neededInStory) Put(varName, (float)value);
                        else PutKeeper(varName, (float)value);
                    } else if(value.type == ValueType.String) {
                        if(neededInStory) Put(varName, (string)value);
                        else PutKeeper(varName, (string)value);
                    }
                });
            }
        } else {
            // else inject existing keeper
            _keeperDict.ForEach((varName, value) => {
                    bool neededInStory = story.variablesState.GlobalVariableExistsWithName(varName);
                    if(!neededInStory) return;
                    if(value.type == ValueType.Bool) {
                        Put(varName, (bool)value);
                    } else if(value.type == ValueType.Int) {
                        Put(varName, (int)value);
                    } else if(value.type == ValueType.Float) {
                        Put(varName, (float)value);
                    } else if(value.type == ValueType.String) {
                        Put(varName, (string)value);
                    }
            });
        }
    }

    private void SaveVariables() {
        // dump all keeper to saveFile
        _keeperDict.ForEach((varName, value) => SaveUtil.SaveValue(varName, value));
    }
    private string ExplainCondition(string condition) {
        varNameBucket?.GetDictionary().ForEach((varName, newName) => {
            condition = condition.Replace(varName, newName).Replace('_', ' ');
        });
        return condition;
    }

    public Optional<Keeper> Get(string varName) {
            if(_keeperDict.TryGetValue(varName, out Keeper value)) {
                return value;
            }
            DebugUtil.DebugError(varName + " not found", name);
            return Optional<Keeper>.None();
    }
    private Keeper PutKeeper<T>(string varName, T value) {
            if(_keeperDict.TryGetValue(varName, out Keeper existing)) {
                existing.Set(value);
                return existing;
            }
            var keeper = Keeper.Of(value);
            _keeperDict.Add(varName, keeper);
#if UNITY_EDITOR
            stats[varName] = keeper.ToString();
#endif
            return keeper;
        }

    public void Put<T>(string varName, T value) {
        _currentStory.variablesState[varName] = value;
    }

    public void Compute<T>(string varName, string operatorStr, T value) {
        if(_keeperDict.TryGetValue(varName, out Keeper existing)) {
            T newValue = existing.Compute(operatorStr,value);
            Put(varName, newValue);
#if UNITY_EDITOR
            stats[varName] = existing.ToString();
#endif
            return;
        }
        throw new KeyNotFoundException(varName + " not found, compute error");
    }

    public bool Compare<T>(string varName, string logicOperator, T value) {
        if(_keeperDict.TryGetValue(varName, out Keeper existing)) {
            return existing.Is(logicOperator,value);
        }
        throw new KeyNotFoundException(varName + " not found, comparison error");
    }
#endregion
#region Log
    [RuntimeObject] public readonly List<string> log = new ();
    private void SaveLog(string line) {
        if(saveLog) log.Add(line);
    }

#endregion

}

public enum InkStage {
    READY, WAIT_CHOICE, END, 
    WAIT_TASK// won't show in this inspector
}

