using System.Linq;
using System.Threading.Tasks;
using EditorAttributes;
using KBCore.Refs;
using UnityEngine;

public class CameraObstacle : MonoBehaviour
{
    // [SerializeField] private float _duration = 0.5f;
    // [SerializeField,Range(0,1)] private float _alpha = 0;
    [SerializeField, MessageBox("Alpha is not in use when transparent material is set", nameof(_replaceMat), MessageMode.Log)]
    private Void messageBox1;
    [SerializeField] Transform _player;

    private bool _alterExistingMat => _transparentMaterial == null;
    private bool _replaceMat => _transparentMaterial != null;

    [SerializeField] Material _transparentMaterial;
    [SerializeField, MessageBox("If Empty, change existing material Transparency", nameof(_alterExistingMat), MessageMode.Log)]
    private Void messageBox2;

    private Transform _mainCam;

    private bool _hidden = false;
    [SerializeField, Child] MeshRenderer[] meshRenders;
    private (MeshRenderer,Material)[] _meshRenders;

#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
    
    private void Start() {
        _mainCam = Camera.main.transform;
        _meshRenders = meshRenders.Select(mr=> (mr, new Material(mr.material))).ToArray();
    }

    private void LateUpdate() {
        Parallel.ForEach(_meshRenders, m => {
            Transform tf = m.Item1.transform;
            if(IsObstacleBetweenCameraAndPlayer(tf)) {
                if(_alterExistingMat) Hide();
                else {
                    _targetLerp = 1;
                    ChangeMaterial(m.Item1, m.Item2);
                }
            } else {
                if(_alterExistingMat) Show();
                else {
                    _targetLerp = 0;
                    ChangeMaterial(m.Item1, m.Item2);
                }
            }
        });
    }

    private bool IsObstacleBetweenCameraAndPlayer(Transform tf) {
        var camToObstacle = tf.position - _mainCam.position;
        camToObstacle.y = 0;
        var sqrCamToObstacle = Vector3.SqrMagnitude(camToObstacle);
        if(sqrCamToObstacle < 1) return true; // too close to cam

        var camToPlayer = _player.position - _mainCam.position;
        camToPlayer.y = 0;
        var sqrCamToPlayer = Vector3.SqrMagnitude(camToPlayer);

        if(sqrCamToObstacle > sqrCamToPlayer) return false;

        var prj = Vector3.Project(camToObstacle, camToPlayer);

        var dist = camToPlayer.z==0? (prj.x / camToPlayer.x) : (prj.z / camToPlayer.z);

        if(dist <= 0 || dist > 1) return false;

        return Vector3.Angle(camToPlayer, camToObstacle) < 10;
    }

    float _lerp = 0;
    float _targetLerp = 0;
    public void ChangeMaterial(MeshRenderer mr, Material material) {
        if(_targetLerp == _lerp) return;
        // Debug.Log(_lerp);
        // if(_targetLerp > _lerp) _lerp = math.min(_lerp + Time.deltaTime / _duration, 1);
        // else _lerp = math.max(_lerp - Time.deltaTime / _duration, 0);

        // foreach(var mr in _meshRenders) {
        //     mr.Item1.material.Lerp(mr.Item2, _transparentMaterial, _lerp);
        // }

        if(_targetLerp == 1) mr.material = _transparentMaterial;
        else mr.material = material;
        _lerp = _targetLerp;

    }

    public void Hide() {
        if(_hidden) return;
        Debug.Log("hide");
        foreach(var mr in _meshRenders) {
            var m = new Material(mr.Item2);
            m.SetFloat("_Surface", 1);
            m.SetColor("_BaseColor", new Color( m.color.r, m.color.g, m.color.b, 0.1f));
            mr.Item1.material = m;
        }
        _hidden = true;
    }

    public void Show() {
        if(!_hidden) return;
        Debug.Log("show");
        foreach(var mr in _meshRenders) {
            mr.Item1.material = mr.Item2;
        }
        _hidden = false;
    }
}
