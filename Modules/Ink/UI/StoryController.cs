using ModularFramework.Modules.Note;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
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
            SingletonRegistry<InkSystemSO>.Get()
                .Do(so =>
                {
                    so.StartStory(storyName);
                    so.LoadScene();
                    so.Next();
                });
            SingletonRegistry<NoteSystemSO>.Get().Do(sys => sys.LoadNotes());
        }

        public void SkipOrNext()
        {
            InkUIIntegrationSO inkUI = _inkUI.Get();
            if (inkUI.CanSkipOrNext) inkUI.SkipOrNext();
        }

        public void Pause()
        {
            InkUIIntegrationSO inkUI = _inkUI.Get();
            inkUI.PauseAutoPlay();
            Time.timeScale = 0;
        }

        public void Resume()
        {
            InkUIIntegrationSO inkUI = _inkUI.Get();
            inkUI.ResumeAutoPlay();
            Time.timeScale = 1;
        }

        public bool CanSave()
        {
            var inkSys = SingletonRegistry<InkSystemSO>.Get();
            return inkSys.HasValue && inkSys.Get().CanSave;
        }
    }
}