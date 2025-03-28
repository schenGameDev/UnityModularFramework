using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework
{
    public abstract class SceneTransitionSO : ScriptableObject
    {
        [SerializeField] protected float duration = 1;
        public abstract void Transition(CancellationToken token, RawImage lastSceneSnapshot);
    }
}
