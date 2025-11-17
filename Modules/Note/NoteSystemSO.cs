using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

/// <summary>
/// load and save quests
/// </summary>
[CreateAssetMenu(fileName = "NoteSystem_SO", menuName = "Game Module/Note System")]
public class NoteSystemSO : GameSystem<NoteSystemSO>
{
    [Header("Config")] [SerializeField] private Bucket noteBucket;

    [SerializeField] private EventChannel<string> noteChannel;
    [SerializeField] private EventChannel<string> activityChannel;
    [RuntimeObject] private readonly List<string> _notes = new();

    private void OnEnable()
    {
        noteChannel?.AddListener(AddNote);
    }

    private void OnDisable()
    {
        noteChannel?.RemoveListener(AddNote);
    }

    protected override void OnStart()
    {
        LoadNotes();
    }

    protected override void OnAwake() { }
    protected override void OnDestroy() { }

    public void SaveNotes()
    {
        if(_notes.NonEmpty()) SaveUtil.SaveState(NoteConstants.KEY_NOTE, JsonUtility.ToJson(_notes));

    }

    public void LoadNotes()
    {
        SaveUtil.GetState(NoteConstants.KEY_NOTE)
            .Do(json => JsonUtility.FromJson<List<string>>(json)
                .ForEach(AddNote));
    }

    public void AddNote(string noteId)
    {
        if(_notes.Contains(noteId)) return;
        _notes.Add(noteId);
        activityChannel?.Raise(noteBucket?.Get(noteId).Get());
    }
    
    public List<string> GetNotes()
    {
       return _notes.Select(noteId=> noteBucket?.Get(noteId))
            .Where(note => note is { HasValue: true })
            .Select(note => note.Value.Get())
            .ToList();
    }

}