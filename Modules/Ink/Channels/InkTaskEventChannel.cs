using System;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(menuName = "Event Channel/Ink Task Event Channel", fileName = "InkTaskChannel")]
    public class InkTaskEventChannel : EventChannel<(string, string, Action<string>)>
    {
    }
}