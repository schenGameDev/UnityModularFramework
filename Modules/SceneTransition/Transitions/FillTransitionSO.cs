using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "FillTransition_SO", menuName = "Game Module/Scene Transition/Fill")]
public class FillTransitionSO : SceneTransitionSO
{
    [SerializeField] private Image maskPrefab;
    [SerializeField] private Image.FillMethod fillMethod;
    [SerializeField, Rename("Start From Left/Bottom/Clockwise")] private bool fromLeftBottomClockwise;

    public override void Transition(CancellationToken token, RawImage lastSceneSnapshot)
    {
        Fill(token, lastSceneSnapshot).Forget();
    }
    
    async UniTaskVoid Fill(CancellationToken token, RawImage lastSceneSnapshot) {
        Canvas parentCanvas = lastSceneSnapshot.canvas;
        
        var mask = Instantiate(maskPrefab, parentCanvas.transform);
        mask.type = Image.Type.Filled;
        mask.fillMethod = fillMethod;
        mask.fillAmount = 0;
        mask.fillClockwise = fromLeftBottomClockwise;
        mask.color.SetAlpha(1);
        
        // old scene out
        float t = 0;
        while(t<=duration) {
            mask.fillAmount = t / duration;
            t += Time.deltaTime;
            await UniTask.NextFrame(cancellationToken: token);
        }
        lastSceneSnapshot.color.SetAlpha(0);
        
        // new scene in
        t = 0;
        mask.fillClockwise = !fromLeftBottomClockwise;
        while(t<=duration) {
            mask.fillAmount = 1 - t / duration;
            t += Time.deltaTime;
            await UniTask.NextFrame(cancellationToken: token);
        }
        Destroy(mask.gameObject);
    }
}