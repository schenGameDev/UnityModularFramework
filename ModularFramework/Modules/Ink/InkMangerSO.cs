using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using ModularFramework;
using UnityEngine;
using EditorAttributes;
using System;
using ModularFramework.Utility;
using ModularFramework.Commons;
using AYellowpaper.SerializedCollections;
using UnityEngine.Serialization;
using ValueType = ModularFramework.Commons.ValueType;

/// <summary>
/// load and save story, send and receive variables.
/// read var from story into memory -> any update go in story -> save story state -> clean all memory
/// </summary>
[CreateAssetMenu(fileName = "InkManager_SO", menuName = "Game Module/Ink")]
public class InkManagerSO : GameModule
{
    static readonly string INK_FUNCTION_DO_TASK = "doTask";

    [Header("Config")]
    [SerializeField] private InkStoryBucket stories;
    [SerializeField] private InkTagDefBucket[] tagDefBuckets;
    [SerializeField] private Bucket varNameBucket;
    [SerializeField] private bool saveLog;

    [FoldoutGroup("Event Channels", nameof(inkTextChannel), nameof(varChangeChannel), nameof(chapterChangeEventChannel), nameof(inkTaskChannel))]
    [SerializeField] private EditorAttributes.Void eventChannelGroup;
    [HideInInspector,SerializeField] EventChannel<string>  chapterChangeEventChannel;
    [HideInInspector,SerializeField] EventChannel<(string,Keeper)> varChangeChannel;
    [HideInInspector,SerializeField] EventChannel<Either<InkLine,InkChoice>> inkTextChannel;
    [HideInInspector,SerializeField] EventChannel<(string,string)> inkTaskChannel;

    [Header("Runtime")]
    [Rename("Current Story"),ReadOnly,SerializeField,RuntimeObject] string currentStoryName;
    [ReadOnly,SerializeField,RuntimeObject] private string currentChapter;

#if UNITY_EDITOR
    [ReadOnly,SerializeField,SerializedDictionary,RuntimeObject] private SerializedDictionary<string,string> stats;
#endif
    // variable first save to story, then populate keeper through delegete, then alert unity through event channel
    [RuntimeObject] private Dictionary<string,Keeper> _keeperDict = new();
    [RuntimeObject(cleaner:nameof(ClearStory))] private Story _currentStory;
    [SerializeField,ReadOnly] InkStage stage;
    [RuntimeObject] InkLine _currentLine;
    [RuntimeObject] InkChoice _currentChoice;


    public InkManagerSO() {
        updateMode = UpdateMode.NONE;
        crossScene = true;
    }

    public override void OnFinalDestroy() {
        base.OnFinalDestroy();
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
            (string varName, object newValue) => {
                if(newValue == _keeperDict[varName]) return;
                PutKeeper(varName, newValue);
                varChangeChannel?.Raise((varName, PutKeeper(varName, newValue)));
            });

        _currentStory.onError += (msg, type) => {
            if( type == Ink.ErrorType.Warning )
                DebugUtil.Warn(msg);
            else
                DebugUtil.Error(msg);
        };
    }

    private void ClearStory() {
        _currentStory?.RemoveVariableObserver();
        _currentStory = null;
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
    public bool Next(int choiceIndex = -1) { // next button clickable only at READY state
        if(TaskRunning()) {
            _lastChoiceIndex = choiceIndex;
            return true;
        }
        _lastChoiceIndex = -1;
        if(stage == InkStage.READY) {
            _currentChoice = null;
            _currentLine = null;
            // check if story end or choice ahead
            if(!CanContinue()) {
                _currentChoice = NextChoice();
                if(_currentChoice == null) {
                    stage = InkStage.END;
                    return false;
                } else {
                    inkTextChannel.Raise(Either<InkLine,InkChoice>.FromRight(_currentChoice));
                    stage = InkStage.WAIT_CHOICE;
                }
            } else {
                string text = _currentStory.Continue();
                List<InkTag> tags = _currentStory.currentTags==null? new() : _currentStory.currentTags.Select(t=>InkTag.Of(t,tagDefBuckets,Get)).ToList();
                _currentLine = new InkLine(text,tags);
                SaveLog(text);
                inkTextChannel.Raise(Either<InkLine,InkChoice>.FromLeft(_currentLine));
                stage = InkStage.READY;
            }
            return true;
        }

        if(stage == InkStage.WAIT_CHOICE) {
            if(choiceIndex<0 || choiceIndex>=_currentChoice.choices.Count) {
                throw new ArgumentOutOfRangeException("");
            }
            _currentChoice = null;
            _currentStory.ChooseChoiceIndex(choiceIndex);
            stage = InkStage.READY;
            return true;
        }

        throw new Exception("Wrong place to be");
    }

    // public string NextChunk() { // load all until meeting a choice or end
    //     return _currentStory.ContinueMaximally();
    // }

    private InkChoice NextChoice() {
        if(_currentStory.currentChoices == null) return null;

        List<InkLine> choices = new();

        for(int i=0;i<_currentStory.currentChoices.Count;i++) {
            var choice = _currentStory.currentChoices[i];
            string text = choice.text;
            List<InkTag> tags =choice.tags==null? new() : choice.tags.Select(t=>InkTag.Of(t,tagDefBuckets,Get)).ToList();
            choices.Add(new(text,tags));
        }
        return new InkChoice(choices,ExplainCondition);
    }

    private void LoadStory(string storyName, Story story) {
        SaveUtil.GetState(storyName)
            .Do(states => story.state.LoadJson(states))
            .OrElseDo(story.ResetState);
        InjectVariables(story);
        story.variablesState.ForEach(varName => PutKeeper(varName,story.variablesState[varName]));
        story.BindExternalFunction(INK_FUNCTION_DO_TASK, (string task, string parameter, bool isBlocking) => DoTask(name, parameter, isBlocking));
    }

    public void SaveStory(string storyName, Story story) {
        string saveJson =story.state.ToJson();
        SaveUtil.SaveState(storyName, saveJson);
        SaveVariables();
    }

    private bool CheckChapterChange(Story story) {
        if(story.state.currentPathString == null) return false;
        var arr =story.state.currentPathString.Split('.',3);
        if(arr.Length==3) {
            string newChapter = arr[0] + "." + arr[1];
            if(currentChapter != newChapter) {
                currentChapter = newChapter;
                chapterChangeEventChannel.Raise(newChapter);
                return true;
            }
        }
        return false;
    }

#endregion
#region Task
    [RuntimeObject] int _tasksRunning = 0;
    [RuntimeObject] readonly List<(string,string)> _taskBuffer = new();
    [RuntimeObject] bool _taskBlocked = false;
    private void DoTask(string taskHandler, string parameter, bool isBlocking) {
        // wait till all tasks finish, then proceed to next
        if(_taskBlocked) {
            _taskBuffer.Add((taskHandler, parameter));
            return;
        }
        if(_taskBuffer.NonEmpty()) {
            _taskBuffer.ForEach(task => inkTaskChannel.Raise(task));
            _taskBuffer.Clear();
        }
        _tasksRunning++;
        inkTaskChannel.Raise((taskHandler, parameter));
        // effectprofile, name:effect(play anim, change BG, in-out style, wait time)
        if(isBlocking) {
            _taskBlocked = true;
        }
    }

    public void TaskComplete() {
        _tasksRunning--;
        if(_tasksRunning == 0 && _taskBlocked) _taskBlocked = false;
        if(_taskBuffer.IsEmpty() && _lastChoiceIndex!=-1) {
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
            condition = condition.Replace(varName, newName);
        });
        return condition;
    }

    private Optional<Keeper> Get(string name) {
            if(_keeperDict.TryGetValue(name, out Keeper value)) {
                return value;
            }
            DebugUtil.DebugError(name + " not found", this.name);
            return Optional<Keeper>.None();
    }
    private Keeper PutKeeper<T>(string name, T value) {
            if(_keeperDict.TryGetValue(name, out Keeper existing)) {
                existing.Set(value);
                return existing;
            }
            var keeper = Keeper.Of(value);
            _keeperDict.Add(name, keeper);
#if UNITY_EDITOR
            stats[name] = keeper.ToString();
#endif
            return keeper;
        }

    public void Put<T>(string name, T value) {
        _currentStory.variablesState[name] = value;
    }

    public void Compute<T>(string name, string operatorStr, T value) {
        if(_keeperDict.TryGetValue(name, out Keeper existing)) {
            T newValue = existing.Compute(operatorStr,value);
            Put(name, newValue);
#if UNITY_EDITOR
            stats[name] = existing.ToString();
#endif
            return;
        }
        throw new KeyNotFoundException(name + " not found, compute error");
    }

    public bool Compare<T>(string name, string logicOperator, T value) {
        if(_keeperDict.TryGetValue(name, out Keeper existing)) {
            return existing.Is(logicOperator,value);
        }
        throw new KeyNotFoundException(name + " not found, comparison error");
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
    READY, WAIT_CHOICE, END
}

