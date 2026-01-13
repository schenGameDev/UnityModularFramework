using ModularFramework.Utility;
using UnityEngine;
using UnityTimer;

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    [SerializeField] private float time = 0.5f;
    [SerializeField] private float maxZoom = 8;
    
    private Camera _camera;

    private Vector3 _anchorRT, _anchorLB;
    
    private Vector3 _defaultCenter;
    private float _defaultZoom;
    private float _zoom;
    private Vector3 _pos;
    private CountdownTimer _timer;

    
    protected void Awake()
    {
        SingletonRegistry<CameraController2D>.TryRegister(this);
        _camera = GetComponent<Camera>();
        _defaultZoom = _camera.orthographicSize;
        _defaultCenter = _camera.transform.position;
        _zoom = _defaultZoom;
        _anchorLB = _camera.ScreenToWorldPoint(Vector2.zero);
        _anchorRT = _camera.ScreenToWorldPoint(new Vector2(Screen.width -1, Screen.height -1));
    }
    
    public void ZoomIn(Transform center)
    {
        ZoomIn(center.position);
    }
    
    public void ZoomIn(Vector3 center) 
        => Zoom(new Vector3(center.x,center.y,_defaultCenter.z), maxZoom);

    public void ZoomOut()
    {
        Zoom(_defaultCenter, _defaultZoom);
    }

    private void Zoom(Vector3 center, float zoom)
    {
        _timer = new CountdownTimer(time);
        _timer.OnTick += () => ZoomTick(zoom);
        _timer.OnTick += () => MoveTick(center);
        _timer.Start();
    }

    private void ZoomTick(float targetZoom)
    {
        _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, targetZoom, ref _zoom, time);
    }
    private void MoveTick(Vector3 center)
    {
        // aware of boundary
        var lb = _camera.WorldToScreenPoint(_anchorLB);
        if(lb.x > 0 || lb.y > 0) return;
        var rt = _camera.WorldToScreenPoint(_anchorRT);
        if(rt.x < Screen.width -1 || rt.y < Screen.height -1) return;
        
        _camera.transform.position = Vector3.SmoothDamp(_camera.transform.position, center, ref _pos, time);
    }

    public void Reset()
    {
        // immediate
        _camera.orthographicSize = _defaultZoom;
        _camera.transform.position = _defaultCenter;
        
        _zoom = _defaultZoom;
        _pos = _defaultCenter;
        
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}