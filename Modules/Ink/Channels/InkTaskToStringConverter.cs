using System;
using ModularFramework;
using ModularFramework.Commons;
using UnityEngine;

[CreateAssetMenu(fileName = "InkTaskToStringConverter_SO", menuName = "Converters/InkTaskToStringConverter")]
public class InkTaskToStringConverter : EventChannelConverter<(string,string,Action<string>),string>
{
    [SerializeField] private string taskName;
    
    protected override Optional<string> Convert((string, string, Action<string>) message)
    {
        if (message.Item1 == taskName)  return message.Item2;
        return Optional<string>.None();
    }
}