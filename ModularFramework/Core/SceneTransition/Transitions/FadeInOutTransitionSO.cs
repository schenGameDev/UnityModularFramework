using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "FadeInOut_SO", menuName = "Game Module/Scene Transition/Fade Out And In")]
public class FadeInOutTransitionSO : SceneTransitionSO<FadeInOutTransitionSO>
{
    protected enum FadeMode { FADE_IN, FADE_OUT, FADE_OUT_IN }
    [SerializeField] private Image maskPrefab;
    [SerializeField] private FadeMode mode = FadeMode.FADE_OUT_IN;
    
    protected override void OnTransition(CancellationToken token, RawImage lastSceneSnapshot)
    {
        Fade(token, lastSceneSnapshot).Forget();
    }
    
    async UniTaskVoid Fade(CancellationToken token, RawImage lastSceneSnapshot) {
        Canvas parentCanvas = lastSceneSnapshot.canvas;
        
        var mask = Instantiate(maskPrefab, parentCanvas.transform);
        // old scene out
        if (mode != FadeMode.FADE_IN) 
        {
            mask.color = mask.color.SetAlpha(0);
            Color from = mask.color;
            Color to = mask.color.SetAlpha(1);

            float t = 0;
            while(t<=duration) {
                mask.color = Color.Lerp(from, to, t / duration);
                t += Time.deltaTime;
                await UniTask.NextFrame(cancellationToken: token);
            }
            
        }
        lastSceneSnapshot.color = lastSceneSnapshot.color.SetAlpha(0);
        mask.color = mask.color.SetAlpha(1);
        
        // new scene in
        if (mode != FadeMode.FADE_OUT)
        {
            Color from = mask.color;
            Color to = mask.color.SetAlpha(0);
            

            float t = 0;
            while(t<=duration) {
                mask.color = Color.Lerp(from, to, t / duration);
                t += Time.deltaTime;
                await UniTask.NextFrame(cancellationToken: token);
            }
        }
        Destroy(mask.gameObject);
        Finish(lastSceneSnapshot);
    }
}
