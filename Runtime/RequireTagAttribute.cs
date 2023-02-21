using System;

namespace Ikonoclast.ClassAttributes
{
    /// <summary>
    /// The RequireTag attribute automatically sets a GameObject with the given tag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequireTagAttribute : Attribute, IClassAttribute
    {
        public string tag { get; }

        public bool createIfNotDefined { get; }

        public RequireTagAttribute(string requiredTag, bool createIfNotDefined = true)
        {
            if (string.IsNullOrWhiteSpace(requiredTag))
                throw new Exception($"{nameof(requiredTag)} must be a non-null, non-whitespace string.");

            tag = requiredTag;
            this.createIfNotDefined = createIfNotDefined;
        }
    }
}
