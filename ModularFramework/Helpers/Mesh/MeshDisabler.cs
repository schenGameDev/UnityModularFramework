using System.Linq;
using EditorAttributes;
using UnityEngine;

public class MeshDisabler : MonoBehaviour
{
    [SerializeField] float maxHeight;
    [Button]
    private void DisableMeshes()
    {
        foreach(var mc in gameObject.GetComponentsInChildren<MeshCollider>()
                    .Where(mc=>mc.transform.position.y > maxHeight && !MeshCombiner.CannotMerge(mc.transform))) mc.enabled = false;
        // foreach (var mr in gameObject.GetComponentsInChildren<MeshRenderer>().Where(mr=>!MeshCombiner.CannotMerge(mr.transform))) mr.enabled = false;
        // foreach (var fe in gameObject.GetComponentsInChildren<FlickeringEmissive>()) fe.enabled = false;
    }
    
    [Button]
    private void EnableMeshes()
    {
        EnableAllChildren(transform);
        foreach (var mc in gameObject.GetComponentsInChildren<MeshCollider>())
        {
            mc.gameObject.SetActive(true); mc.enabled = true;
        }

        foreach (var mr in gameObject.GetComponentsInChildren<MeshRenderer>())
        {
            mr.gameObject.SetActive(true); mr.enabled = true;
        }
    }
    
    [Button]
    private void StaticShadowCaster()
    {
        foreach (var mr in GetComponentsInChildren<MeshRenderer>().Where(mr => mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off))
        {
            mr.staticShadowCaster = true;
        }
        Debug.Log(gameObject.name + " shadow caster set to " + true);
    }
    
    [Button]
    private void Shadow(bool on)
    {
        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        {
            mr.shadowCastingMode = on? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        }
        Debug.Log(gameObject.name + " shadow " + (on? "on":"off"));
    }
    
    private void EnableAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (!child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
            }
            EnableAllChildren(child);
        }
    }
}