using System;
using ModularFramework;
using UnityEngine;

[RequireComponent(typeof(Marker))]
public class InkPrinter : TextPrinter,IMark
{
    public Type[][] RegistryTypes => new[] { new []{typeof(InkUIIntegrationSO)}};
}