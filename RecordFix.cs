
// This is needed to enable the record feature in .NET framework and .NET core <= 3.1 projects

// From:
// https://youtrack.jetbrains.com/issue/RSRP-488012/The-predefined-type-System.Runtime.CompilerServices.IsExternalInit-must-be-defined-or-imported-in-order-to-declare-init-only


// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}