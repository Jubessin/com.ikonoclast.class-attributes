using System;

namespace Ikonoclast.ClassAttributes
{
    /// <summary>
    /// The RequireName attribute automatically sets the game object name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RequireNameAttribute : Attribute, IClassAttribute
    {
        public string name { get; }

        public RequireNameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new NullReferenceException(nameof(name));

            this.name = name;
        }
    }
}
