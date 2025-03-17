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

/// <summary>
/// load and save story, send and receive variables.
/// read var from story into memory -> any update go in story -> save story state -> clean all memory
/// </summary>
[CreateAssetMenu(fileName = "InkManager_SO", menuName = "Game Module/Ink")]
public class InkManagerSO : GameModule
{
    static readonly string INK_FUNCTION_DO_TASK = "doTask";

    [Header("Config")]
    [SerializeField] private InkStoryBucket _stories;
    [SerializeField] private InkTagDefBucket[] _tagDefBuckets;
    [SerializeField] private Bucket _varNameBucket;

    [FoldoutGroup("Event Channels", nameof(_inkTextChannel), nameof(_varChangeChannel), nameof(_chapterChangeEventChannel), nameof(_inkTaskChannel))]
    [SerializeField] private EditorAttributes.Void _eventChannelGroup;
    [HideInInspector,SerializeField] EventChannel<string>  _chapterChangeEventChannel;
    [HideInInspector,SerializeField] EventChannel<(string,Keeper)> _varChangeChannel;
    [HideInInspector,SerializeField] EventChannel<Either<InkLine,InkChoice>> _inkTextChannel;
    [HideInInspector,SerializeField] EventChannel<(string,string)> _inkTaskChannel;

    [Header("Runtime")]
    [Rename("Current Story"),ReadOnly,SerializeField,RuntimeObject] string _currentStoryName;
    [ReadOnly,SerializeField,RuntimeObject] private string _currentChapter;

#if UNITY_EDITOR
    [ReadOnly,SerializeField,SerializedDictionary,RuntimeObject] private SerializedDictionary<string,string> _stats;
#endif
    // variable first save to story, then populate keeper through delegete, then alert unity through event channel
    [RuntimeObject] private Dictionary<string,Keeper> _keeperDict = new();
    [RuntimeObject(cleaner:nameof(ClearStory))] private Story _currentStory;
    [SerializeField,ReadOnly] InkStage _stage;
    [RuntimeObject] InkLine _currentLine;
    [RuntimeObject] InkChoice _currentChoice;


    public InkManagerSO() {
        updateMode = UpdateMode.NONE;
    }

    public override void OnDestroy() {
        base.OnDestroy();
        _stage = InkStage.END;
    }

    #region Story
    public void StartStory(string name)
    {
        _stage = InkStage.READY;
        _currentStory = new Story(_stories.Get(name).Get().text);

#if UNITY_EDITOR
        _currentStoryName = name;
#endif
        LoadStory(_currentStory);

        _currentStory.ObserveVariables(new List<string>(_keeperDict.Keys),
            (string varName, object newValue) => {
                if(newValue == _keeperDict[varName]) return;
                PutKeeper(varName, newValue);
                _varChangeChannel?.Raise((varName, PutKeeper(varName, newValue)));
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


    public bool Next(int choiceIndex = -1) { // next button clickable only at READY state
        if(TaskRunning()) return true;

        if(_stage == InkStage.READY) {
            _currentChoice = null;
            _currentLine = null;
            // check if story end or choice ahead
            if(!CanContinue()) {
                _currentChoice = NextChoice();
                if(_currentChoice == null) {
                    _stage = InkStage.END;
                    return false;
                }
            } else {
                string text = _currentStory.Continue();
                List<InkTag> tags = _currentStory.currentTags==null? new() : _currentStory.currentTags.Select(t=>InkTag.Of(t,_tagDefBuckets,Get)).ToList();
                _currentLine = new InkLine(text,tags);
            }

            if(CheckChapterChange(_currentStory)) { // render chapter change
                _stage = InkStage.WAIT_CHAPTER_CHANGE;
                return true;
            }
        }

        if(_stage == InkStage.WAIT_CHAPTER_CHANGE || _stage == InkStage.READY) { // render text
            if(_currentChoice == null) {
                _inkTextChannel.Raise(Either<InkLine,InkChoice>.FromRight(_currentChoice));
                _stage = InkStage.WAIT_CHOICE;
            } else {
                _inkTextChannel.Raise(Either<InkLine,InkChoice>.FromLeft(_currentLine));
                _stage = InkStage.READY;
            }
            return true;
        }

        if(_stage == InkStage.WAIT_CHOICE) {
            if(choiceIndex<0 || choiceIndex>=_currentChoice.choices.Count) {
                throw new ArgumentOutOfRangeException("");
            }
            _currentChoice = null;
            _currentStory.ChooseChoiceIndex(choiceIndex);
            _stage = InkStage.READY;
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
            List<InkTag> tags =choice.tags==null? new() : choice.tags.Select(t=>InkTag.Of(t,_tagDefBuckets,Get)).ToList();
            choices.Add(new(text,tags));
        }
        return new InkChoice(choices,ExplainCondition);
    }

    private void LoadStory(Story story) {
        SaveUtil.Get(EnvironmentConstants.STORY_STATE)
            .Do(states => story.state.LoadJson(states))
            .OrElseDo(story.ResetState);

        story.variablesState.ForEach(varName => PutKeeper(varName,story.variablesState[varName]));
        story.BindExternalFunction(INK_FUNCTION_DO_TASK, (string task, string parameter) => DoTask(name, parameter));
    }

    public void SaveStory(Story story) {
        string saveJson =story.state.ToJson();
        SaveUtil.Save(EnvironmentConstants.STORY_STATE, saveJson);
    }

    private bool CheckChapterChange(Story story) {
        if(story.state.currentPathString == null) return false;
        var arr =story.state.currentPathString.Split('.',3);
        if(arr.Length==3) {
            string newChapter = arr[0] + "." + arr[1];
            if(_currentChapter != newChapter) {
                _currentChapter = newChapter;
                _chapterChangeEventChannel.Raise(newChapter);
                return true;
            }
        }
        return false;
    }

#endregion
#region Task
    [RuntimeObject] int _tasksRunning = 0;
    private void DoTask(string taskHandler, string parameter) {
        // wait till all tasks finish, then proceed to next
        _tasksRunning ++;
        _inkTaskChannel.Raise((taskHandler, parameter));
        // effectprofile, name:effect(play anim, change BG, in-out style, wait time)
    }

    public void TaskComplete() {
        _tasksRunning --;
    }

    public bool TaskRunning() => _tasksRunning > 0;

#endregion

#region Variables
    private string ExplainCondition(string condition) {
        _varNameBucket?.GetDictionary().ForEach((varName, name) => {
            condition.Replace(varName, name);
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
            _stats[name] = keeper.ToString();
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
            _stats[name] = existing.ToString();
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
}

public enum InkStage {
    READY, WAIT_CHAPTER_CHANGE, WAIT_CHOICE, END
}

