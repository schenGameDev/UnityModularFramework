using System.Linq;
using EditorAttributes;
using UnityEngine;
using Polygon2D;
using System.Collections.Generic;
using ModularFramework.Utility;
using static Polygon2D.Utility;

public class Test : MonoBehaviour
{
    [SerializeField] string polygonA;
    [SerializeField,TextArea] string polygonB;

    Polygon _a;
    List<Polygon> _bs = new();

    [Button]
    public void ACutB() {
        _a = new Polygon(parseString(polygonA));
        _bs = polygonB.Split("\n").Select(b => new Polygon(parseString(b),false)).ToList();

        List<Polygon> result = Cut(_a,_bs);
        result.ForEach(r=>DebugUtil.Log(r));
    }

    [Button]
    public void AMergeB() {
        _a = new Polygon(parseString(polygonA));
        _bs = polygonB.Split("\n").Select(b => new Polygon(parseString(b),false)).ToList();

        List<Polygon> result = Merge(_bs,_a);
        result.ForEach(r=>DebugUtil.Log(r));
    }

    [SerializeField] string polygonC;
    [Button]
    public void CSelfCross() {
        Polygon c = new Polygon(parseString(polygonC));
        List<Polygon> result = CheckSelfCross(c);
    }

    private List<Vector2> parseString(string vertexString) {
        string[] temp = vertexString.Trim().Replace("(","").Replace(")","").Split(",");
        List<Vector2> res = new();
        for(int i = 0; i<temp.Length; i+=2) {
            res.Add(new Vector2 (float.Parse(temp[i]), float.Parse(temp[i+1])));
        }
        return res;
    }

     private Vector2 parseVector2(string vector2) {
        string[] temp = vector2.Substring(1,vector2.Length - 2).Split(",");
        return new Vector2 (float.Parse(temp[0]), float.Parse(temp[1]));
    }
}
