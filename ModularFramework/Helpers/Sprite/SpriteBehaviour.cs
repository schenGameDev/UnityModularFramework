using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public abstract class SpriteBehaviour : MonoBehaviour
{
    protected Image Image { get; private set; }
    protected SpriteRenderer SpriteRenderer { get; private set; }
    protected bool IsImage { get; private set; }
    
    protected virtual void Awake()
    {
        IsImage = TryGetComponent<Image>(out var image);
        if(IsImage) Image = image;
        else SpriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    protected void SetAlpha(float val) {
        if(IsImage) Image.color = Image.color.SetAlpha(val);
        else SpriteRenderer.color = SpriteRenderer.color.SetAlpha(val);
    }
    
    protected float GetAlpha()
    {
        return IsImage? Image.color.a : SpriteRenderer.color.a;
    }
    
    protected void SetSprite(Sprite newSprite)
    {
        if(IsImage) Image.sprite = newSprite;
        else SpriteRenderer.sprite =newSprite;
    }
    
    protected async UniTask FadeTask(bool isFadeIn, float time, CancellationToken token) {
        float t = 0;
        float startAlpha = GetAlpha();
        bool isCancelled = false;
        while(t< time && !isCancelled) 
        {
            SetAlpha(isFadeIn? math.min(1,t/time) : math.max(0,startAlpha-t/time));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        
        SetAlpha(isFadeIn? 1: 0);
    }
}