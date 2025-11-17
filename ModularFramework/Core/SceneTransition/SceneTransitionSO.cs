using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework
{
    public abstract class SceneTransitionSO<T> : SceneTransitionSO where T : SceneTransitionSO<T>
    {
        [SerializeField] protected float duration = 1;

        public override void Transition(CancellationToken token, RawImage lastSceneSnapshot)
        {
            ((T)this).OnTransition(token, lastSceneSnapshot);
        }
        
        protected abstract void OnTransition(CancellationToken token, RawImage lastSceneSnapshot);
    }
    
    public abstract class SceneTransitionSO : ScriptableObject
    {
        public abstract void Transition(CancellationToken token, RawImage lastSceneSnapshot);
        protected void Finish(RawImage lastSceneSnapshot)
        {
            lastSceneSnapshot.gameObject.SetActive(false);
        }
    }
}
