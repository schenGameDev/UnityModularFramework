using System;
using ModularFramework;
using UnityEngine;

[CreateAssetMenu(menuName = "Event Channel/Ink Task Event Channel", fileName = "InkTaskChannel")]
public class InkTaskEventChannel : EventChannel<(string,string,Action<string>)> { }