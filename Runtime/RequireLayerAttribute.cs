using System;
using UnityEngine;

namespace Ikonoclast.ClassAttributes
{
    /// <summary>
    /// The RequireLayer attribute automatically sets the specified required layer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequireLayerAttribute : Attribute, IClassAttribute
    {
        public int layer { get; }

        public RequireLayerAttribute(string requiredLayerName)
        {
            layer = LayerMask.NameToLayer(requiredLayerName);
        }
    }
}
