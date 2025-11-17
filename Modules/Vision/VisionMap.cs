
using System.Collections.Generic;
using ModularFramework;
using static ModularFramework.Utility.MathUtil;
using UnityEngine;
using System.Linq;
using EditorAttributes;
using ModularFramework.Commons;
using Polygon2D;
/// <summary>
/// Life cycle: OnAwake(Reset -> Prepare) -> OnUpdate(CalculateVision -> ImplementVision)
/// </summary>
public abstract class VisionMap : GameModule<VisionMap>, IRegistrySO
{
    [Header("Map Config")]
    [SerializeField] protected Vector2Int center;
    [SerializeField] protected Vector2Int dimension;
    [SerializeField,Suffix("Always visible to player")] protected float groundLevel = 0;
    [SerializeField, Range(0.05f, 1f)] protected float stepSize = 0.05f;
    [SerializeField] protected string[] _layers;
    [SerializeField] protected float mapHeight = 100;
    [SceneRef("VISION_MASK_PARENT")] protected Transform maskParent;


    [Header("Vision Config")]
    public float PeripheralVisionRadius = 2;
    public float ConeVisionDistance = 10;
    [Suffix("Dividable by 20")]public int ConeAngle = 60;
    public float EyeHeight;

    [Header("Dim Config")]
    [SerializeField] protected bool isDim = true;
    [SerializeField, ShowField(nameof(isDim))] protected float maxRange = 30;
    [SerializeField, ShowField(nameof(isDim))] protected float stayVisibleTime = 5;
    [SerializeField, ShowField(nameof(isDim))] protected float dimTime = 2;


    [SceneRef("PLAYER")] protected Transform Player;

    [RuntimeObject] protected float skippedTime;

    [Header("Event Channel")]
    [SerializeField] private EventChannel<bool> _openEyeEvent;
    public bool Active;

    private void OnEnable() {
        if(_openEyeEvent != null) {
            _openEyeEvent.AddListener(SetActive);
        }
    }

    private void OnDisable() {
        if(_openEyeEvent != null) _openEyeEvent.RemoveListener(SetActive);
    }

    public void SetActive(bool isEyeOpen) => Active = !Active;

    protected override void OnAwake()
    {
        Prepare();
        Reset();
        Active = true;
    }

    protected override void OnStart() { }

    protected override void OnUpdate()
    {
        if(!_flip) {
            skippedTime += DeltaTime;
            CalculateVision();
        } else {
            ImplementVision();
            skippedTime = DeltaTime;
        }
    }
    protected override void OnDestroy() { }
    protected override void OnDraw() { }


    protected virtual void Reset()
    {
        
    }

    protected abstract void Prepare();

    [RuntimeObject] private Flip _flip = new();

    protected abstract void CalculateVision();

    protected abstract void ImplementVision();

#region Blocker
    [RuntimeObject] private HashSet<VisionBlocker> _blockers = new();
    public void Register(Transform tf) {
        VisionBlocker blocker = tf.GetComponent<VisionBlocker>();
        if(_blockers.Contains(blocker)) return;
        _blockers.Add(blocker);
    }

    public void Unregister(Transform tf) {
        VisionBlocker blocker = tf.GetComponent<VisionBlocker>();
        if(!_blockers.Contains(blocker)) return;
        _blockers.Remove(blocker);
    }

    protected HashSet<Polygon> GetBlockers(Vector2Int center, float distance) {
        float h = Player.position.y + EyeHeight;
        float sqrDistance = distance * distance;
        float sqrDistanceExt = (distance + 2) * (distance + 2);
        HashSet<Polygon> blockers = _blockers
            .Where(b=>Vector2.SqrMagnitude(NearestPoint(b.transform.position) - center) < sqrDistanceExt)
            .Select(b=> b.GetVertices(h))
            .Where(arr=> arr!=null && arr.Count() >2)
            .Select(arr => arr.Select(v=> (Vector2) NearestPoint(v)).ToList())
            .Select(arr=> new Polygon(arr))
            .Where(pol => pol.IsValid)
            .Select(pol => pol.GetFacingDisect(center))
            .Where(l=>Vector2.SqrMagnitude(l.A-center) <sqrDistance || Vector2.SqrMagnitude(l.B-center) <sqrDistance)
            .Select(l=> new Polygon(new() {l.A,l.B, (l.B - center)* sqrDistance + l.B, (l.A - center)* sqrDistance + l.A}))
            .Where(pol=>pol.IsValid)
            .ToHashSet();
        return blockers;
    }
#endregion

#region Util

    protected Vector2Int NearestPoint(Vector3 worldPoint) {
        var deltaXFromCenter = Round((worldPoint.x - center.x) / stepSize);
        var deltaYFromCenter = Round((worldPoint.z - center.y) / stepSize);

        return new Vector2Int(deltaXFromCenter, deltaYFromCenter);
    }

    protected Vector2Int NearestPoint(Vector2 worldPoint) {
        var deltaXFromCenter = Round((worldPoint.x - center.x) / stepSize);
        var deltaYFromCenter = Round((worldPoint.y - center.y) / stepSize);

        return new Vector2Int(deltaXFromCenter, deltaYFromCenter);
    }
    protected Vector3 TranslateToWorldPoint(Vector2Int point, float height) {
        return new Vector3(point.x * stepSize + center.x, height, point.y * stepSize + center.y);
    }

    protected Vector3 TranslateToRelativeWorldPoint(Vector2 p) {
        return new Vector3(p.x * stepSize, 0, p.y * stepSize);
    }
    protected Vector2 TranslateToScreenPoint(Vector3 worldPoint) => Camera.main.WorldToScreenPoint(worldPoint);

#endregion
}
