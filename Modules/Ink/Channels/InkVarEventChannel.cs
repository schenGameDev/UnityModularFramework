using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(menuName = "Event Channel/Ink Var Event Channel", fileName = "InkVarChannel")]
    public class InkVarEventChannel : EventChannel<(string, Keeper)>
    {
    }
}