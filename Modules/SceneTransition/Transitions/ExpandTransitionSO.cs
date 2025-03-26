using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ExpandTransition_SO", menuName = "Game Module/Scene Transition/Expand")]
public class ExpandTransitionSO : SceneTransitionSO
{
    [SerializeField] private Image maskPrefab;

    public override void Transition(CancellationToken token, RawImage lastSceneSnapshot)
    {
        Expand(token, lastSceneSnapshot).Forget();
    }
    
    async UniTaskVoid Expand(CancellationToken token, RawImage lastSceneSnapshot) {
        Canvas parentCanvas = lastSceneSnapshot.canvas;
        
        var mask = Instantiate(maskPrefab, parentCanvas.transform);
        mask.color.SetAlpha(1);
        var rect = mask.rectTransform;
        
        rect.sizeDelta = Vector2.zero;
        
        float dist = math.sqrt(math.square(Screen.width) + math.square(Screen.height));
        
        // old scene out
        float t = 0;
        while(t<=duration)
        {
            var d = t / duration * dist;
            rect.sizeDelta = new Vector2(d, d);
            t += Time.deltaTime;
            await UniTask.NextFrame(cancellationToken: token);
        }
        lastSceneSnapshot.color.SetAlpha(0);
        
        // new scene in
        t = 0;
        while(t<=duration) {
            var d = (1 - t / duration) * dist;
            rect.sizeDelta = new Vector2(d, d);
            t += Time.deltaTime;
            await UniTask.NextFrame(cancellationToken: token);
        }
        Destroy(mask.gameObject);
    }
}