using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
#if UNITY_EDITOR
using UnityEditor;

#endif
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    [SerializeField] private bool combineColliders;
    [SerializeField] private bool combineRenderers;
    [SerializeField, ShowField(nameof(combineColliders))] private float maxHeight;
    [SerializeField] public GameObject combinedRendererPrefab;
    [SerializeField] public GameObject combinedColliderPrefab;

    private const string folderPath = "Assets/_Dev_Workplace/PawFiles/Mesh";

    private Transform _parent;

    
    [Button]
    private void CombineMeshes()
    {
        _parent = new GameObject(name + " combined mesh").transform;
        RemoveExtraMeshCollider();
        if(combineColliders) MergeColliderMeshesGreedy();
        if(combineRenderers) MergeRendererMeshes();
        
        Debug.Log(name + " combined meshes complete!!!");
    }

    [Button]
    private void EnableMeshes()
    {
        EnableAllChildren(transform);
        if(combineColliders) foreach(var mc in gameObject.GetComponentsInChildren<MeshCollider>()) mc.enabled = true;
        if(combineRenderers) foreach (var mr in gameObject.GetComponentsInChildren<MeshRenderer>()) mr.enabled = true;
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
    
#if UNITY_EDITOR
    void SaveMesh(Mesh mesh, string meshName, bool isCollider, bool optimizeMesh)
    {
        if (string.IsNullOrEmpty(meshName)) return;
        if (!AssetDatabase.IsValidFolder(folderPath + "/" + name))
        {
            AssetDatabase.CreateFolder(folderPath, name);
            AssetDatabase.Refresh();
        }

        string parent = isCollider ? "Collider" : "Renderer";
        if (!AssetDatabase.IsValidFolder(folderPath + "/" + name + "/" + parent))
        {
            AssetDatabase.CreateFolder(folderPath + "/" + name, parent);
            AssetDatabase.Refresh();
        }
            
        string path = folderPath +"/" + name + "/" +parent + "/"+ meshName + ".asset";
        if (optimizeMesh)
            MeshUtility.Optimize(mesh);
    
        AssetDatabase.CreateAsset(mesh, path);
    }
#endif
    
    private void RemoveExtraMeshCollider()
    {
        gameObject.GetComponentsInChildren<MeshCollider>().Where(mc => mc.GetComponents<MeshCollider>().Length > 1)
            .Select(mc=>mc.GetComponents<MeshCollider>()).ToList().ForEach(cs =>
            {
                bool skip = true;
                foreach (MeshCollider mc in cs)
                {
                    if (skip)
                    {
                        skip = false;
                        continue;
                    }
                    DestroyImmediate(mc);
                }
            });
    }
    
    void MergeColliderMeshes()
    {
        MeshCollider[] meshColliders = gameObject.GetComponentsInChildren<MeshCollider>()
            .Where(mc => mc.sharedMesh != null)
            .OrderBy(mc => mc.transform.position.y).ToArray();
        
        if(meshColliders.Length == 0) return;

        int count = 0;

        List<MeshCollider> meshBuffer = new ();
        int vertexCount = 0;
        foreach(var mc in meshColliders)
        {
            if (vertexCount + mc.sharedMesh.vertexCount > 65535)
            {
                CombineInstance[] combine = new CombineInstance[meshBuffer.Count];
                int i = 0;
                while (i < meshBuffer.Count)
                {
                    combine[i].mesh = meshBuffer[i].sharedMesh;
                    combine[i].transform = meshBuffer[i].transform.localToWorldMatrix;
                    i++;
                }

                Mesh mesh = new Mesh();
                mesh.CombineMeshes(combine);
                string meshName = "ColliderMesh" + ++count;
#if UNITY_EDITOR
                SaveMesh(mesh, meshName,true, true);
                InstantiateColliderObject(meshName, mesh);
#endif

                vertexCount = 0;
                meshBuffer.Clear();
            }

            if (mc.transform.position.y > maxHeight)
            {
                mc.enabled = false;
                continue;
            }

            vertexCount += mc.sharedMesh.vertexCount;
            meshBuffer.Add(mc);
            mc.enabled = false;
        }
        
        if (vertexCount > 0)
        {
            CombineInstance[] combine = new CombineInstance[meshBuffer.Count];
            int i = 0;
            while (i < meshBuffer.Count)
            {
                combine[i].mesh =meshBuffer[i].sharedMesh;
                combine[i].transform = meshBuffer[i].transform.localToWorldMatrix;
                i++;
            }
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combine,true);
            string meshName = "ColliderMesh" + ++count;
#if UNITY_EDITOR
            SaveMesh(mesh, meshName, true, true);
            InstantiateColliderObject(meshName, mesh);
#endif

        }
    }
    
    void MergeColliderMeshesGreedy() {
        foreach (var bc in gameObject.GetComponentsInChildren<BoxCollider>()
                     .Where(bc => bc.transform.position.y > maxHeight && !CannotMerge(bc.transform)))
        {
            bc.enabled = false;
        }
        
        
        MeshCollider[] meshColliders = gameObject.GetComponentsInChildren<MeshCollider>()
            .Where(mc => mc.sharedMesh != null && !CannotMerge(mc.transform))
            .OrderBy(mc => mc.transform.position.y).ToArray();
        
        if(meshColliders.Length == 0) return;

        int count = 0;

        List<MeshFilter> meshBuffer = new ();
        int vertexCount = 0;
        foreach(var mc in meshColliders)
        {
            if (vertexCount + mc.sharedMesh.vertexCount > 65535)
            {
                CombineInstance[] combine = new CombineInstance[meshBuffer.Count];
                int i = 0;
                while (i < meshBuffer.Count)
                {
                    combine[i].mesh = meshBuffer[i].sharedMesh;
                    combine[i].transform = meshBuffer[i].transform.localToWorldMatrix;
                    i++;
                }

                Mesh mesh = new Mesh();
                mesh.CombineMeshes(combine);
                string meshName = "ColliderMesh" + ++count;
#if UNITY_EDITOR
                SaveMesh(mesh, meshName,true, true);
                InstantiateColliderObject(meshName, mesh);
#endif

                vertexCount = 0;
                meshBuffer.Clear();
            }

            if (mc.transform.position.y > maxHeight)
            {
                mc.enabled = false;
                
                continue;
            }
            var mf = mc.GetComponent<MeshFilter>();
            vertexCount += mf.sharedMesh.vertexCount;
            meshBuffer.Add(mf);
            mc.enabled = false;
        }
        
        if (vertexCount > 0)
        {
            CombineInstance[] combine = new CombineInstance[meshBuffer.Count];
            int i = 0;
            while (i < meshBuffer.Count)
            {
                combine[i].mesh =meshBuffer[i].sharedMesh;
                combine[i].transform = meshBuffer[i].transform.localToWorldMatrix;
                i++;
            }
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combine,true);
            string meshName = "ColliderMesh" + ++count;
#if UNITY_EDITOR
            SaveMesh(mesh, meshName, true, true);
            InstantiateColliderObject(meshName, mesh);
#endif

        }
    }
    

    void MergeRendererMeshes()
    {
        // group by material
        Dictionary<string, List<MeshFilter>> meshFilters = new ();
        foreach (var mr in gameObject.GetComponentsInChildren<MeshRenderer>().Where(mr=>mr.GetComponent<LODGroup>() == null))
        {
            if(CannotMerge(mr.transform)) continue;
            
            foreach (var m in mr.sharedMaterials)
            {
                string id = m.name;
                if (!meshFilters.TryGetValue(id, out var mf))
                {
                    mf = new();
                
                    meshFilters[id] = mf;
                
                } 
                mf.Add(mr.GetComponent<MeshFilter>());
            }
            
        }
        if (meshFilters.Count == 0) return;

        int count = 0;
        
        foreach (var pair in meshFilters)
        {
            var mfs = pair.Value;
            var materialName = pair.Key;
            bool isMultiMat = materialName.Contains(",");
            
            var materials = isMultiMat? mfs[0].GetComponent<MeshRenderer>().sharedMaterials : 
                mfs[0].GetComponent<MeshRenderer>().sharedMaterials.Where(m=>m.name == materialName).ToArray();
            
            List<Mesh> meshes;
            
            if (materials.Length == 1)
            {
                meshes = CombineMeshOnOneMaterial(mfs, 0);
                
            }
            else
            {
                meshes = new();
                List<List<Mesh>>  submeshes = new();
            
                for(int i = 0; i< materials.Length; i++)
                {
                    submeshes.Add(CombineMeshOnOneMaterial(mfs, i));
                }

                for (int j = 0; j < submeshes[0].Count; j++)
                {
                    CombineInstance[] combine = new CombineInstance[materials.Length];
                    for(int t = 0; t< materials.Length; t++)
                    {
                        combine[t].mesh=submeshes[t][j];
                        combine[t].subMeshIndex = 0;
                        combine[t].transform = Matrix4x4.identity;
                    }
                    Mesh finalMesh = new Mesh();
                    finalMesh.CombineMeshes(combine, false);
                    meshes.Add(finalMesh);
                }
            }
            
#if UNITY_EDITOR
            
            foreach (var ms in meshes)
            {
                string meshName = "ShaderMesh" + ++count;
                SaveMesh(ms, meshName, false, true);
                InstantiateRendererObject(meshName, ms, materials);
            }
#endif
        }

    }

    private List<Mesh> CombineMeshOnOneMaterial(List<MeshFilter> mfs, int materialIndex)
    {
        List<Mesh> meshes = new();
        int vertexCount = 0;
        List<MeshFilter> meshBuffer = new ();
        foreach (var mf in mfs)
        {
            if (vertexCount + mf.sharedMesh.vertexCount > 65535)
            {
                CombineInstance[] combine = new CombineInstance[meshBuffer.Count];
                int i = 0;
                while (i < meshBuffer.Count)
                {
                    combine[i].mesh = meshBuffer[i].sharedMesh;
                    combine[i].subMeshIndex = materialIndex;
                    combine[i].transform = meshBuffer[i].transform.localToWorldMatrix;
                    i++;
                }

                Mesh mesh = new Mesh();
                mesh.CombineMeshes(combine, true);
                
                meshes.Add(mesh);

                vertexCount = 0;
                meshBuffer.Clear();
            }

            vertexCount += mf.sharedMesh.vertexCount;
            meshBuffer.Add(mf);

            
            foreach (var v in mf.GetComponents<MeshRenderer>())
            {
                v.enabled = false;
            }
            

        }

        if (vertexCount > 0)
        {
            CombineInstance[] combine = new CombineInstance[meshBuffer.Count];
            int i = 0;
            while (i < meshBuffer.Count)
            {
                combine[i].mesh = meshBuffer[i].sharedMesh;
                combine[i].transform = meshBuffer[i].transform.localToWorldMatrix;
                combine[i].subMeshIndex = materialIndex;
                i++;
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combine, true);
            meshes.Add(mesh);
        }
        return meshes;
    }

#if UNITY_EDITOR
    private void InstantiateRendererObject(string objectName, Mesh mesh, Material[] materials)
    {

        var node = PrefabUtility.InstantiatePrefab(combinedRendererPrefab, _parent) as GameObject;
        node.GetComponent<MeshFilter>().mesh = mesh;
        var r =  node.GetComponent<MeshRenderer>();
        r.materials = materials;
        // copy script
        //node.AddComponent<FlickeringEmissive>().GetCopyOf(flicker).enabled = true;
        node.name = objectName;
    }
    private void InstantiateColliderObject(string objectName, Mesh mesh)
    {
        var node = PrefabUtility.InstantiatePrefab(combinedColliderPrefab, _parent) as GameObject;
        node.GetComponent<MeshCollider>().sharedMesh = mesh;
        node.name = objectName;
    }
#endif
    
    public static bool CannotMerge(Transform tf)
    {
        // check if contain any forbidden component
        return false;
    }
}
