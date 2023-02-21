using System;

namespace Ikonoclast.ClassAttributes
{
    /// <summary>
    /// The RequireLayer attribute automatically sets the specified required layer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequireLayerAttribute : Attribute, IClassAttribute
    {
        public string layerName { get; }
        public bool createIfNotDefined { get; }

        public RequireLayerAttribute(string requiredLayerName, bool createIfNotDefined = true)
        {
            if (string.IsNullOrWhiteSpace(requiredLayerName))
                throw new Exception($"{nameof(requiredLayerName)} must be a non-null, non-whitespace string.");

            layerName = requiredLayerName;
            this.createIfNotDefined = createIfNotDefined;
        }
    }
}
