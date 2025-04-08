using System.Linq;
using EditorAttributes;
using UnityEngine;

public class ShadowDisabler : MonoBehaviour
{
    [SerializeField] float groundHeight=8f;

    [Button]
    private void EnableShadow() => ChangeShadowMode(true);
    [Button]
    private void DisableShadow() => ChangeShadowMode(false);
    private void ChangeShadowMode(bool isOn)
    {
        foreach (var mr in gameObject.GetComponentsInChildren<MeshRenderer>()
                     .Where(mr => mr.transform.position.y < groundHeight))
        {
            mr.shadowCastingMode = isOn? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
        
}