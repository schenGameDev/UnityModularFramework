using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CrossFade_SO", menuName = "Game Module/Scene Transition/Cross Fade")]
public class CrossFadeTransitionSO : SceneTransitionSO
{
    public override void Transition(CancellationToken token, RawImage lastSceneSnapshot)
    {
        FadeOut(token, lastSceneSnapshot).Forget();
    }
    
    async UniTaskVoid FadeOut(CancellationToken token, RawImage lastSceneSnapshot) {
        Color from = Color.white;
        Color to = new Color(1,1,1,0);
        float t = 0;
        while(t<=duration) {
            lastSceneSnapshot.color = Color.Lerp(from, to, t / duration);
            t += Time.deltaTime;
            await UniTask.NextFrame(cancellationToken: token);
        }
        lastSceneSnapshot.color = to;
    }
}