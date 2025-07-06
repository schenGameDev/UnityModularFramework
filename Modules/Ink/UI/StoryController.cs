using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

public class StoryController : MonoBehaviour
{
    private Autowire<InkUIIntegrationSO> _inkUI = new();
    public void StartGame()
    {
        string storyName = InkConstants.DEFAULT_STORY_NAME;
        SaveUtil.GetState(InkConstants.KEY_CURRENT_STORY).Do(sn => storyName = sn);

        StartStory(storyName);
    }
    
    public void StartStory(string storyName)
    {
        GameRunner.GetSystem<InkSystemSO>()
            .Do(so =>
            {
                so.StartStory(storyName);
                so.Next();
            });
        GameRunner.GetSystem<NoteSystemSO>().Do(sys => sys.LoadNotes());
    }
    
    public void SkipOrNext()
    {
        InkUIIntegrationSO inkUI = _inkUI.Get();
        if (inkUI.CanSkipOrNext) inkUI.SkipOrNext();
    }
}