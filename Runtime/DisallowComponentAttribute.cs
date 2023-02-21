using System;

namespace Ikonoclast.ClassAttributes
{

    /// <summary>
    /// The DisallowComponent attribute automatically removes conflicting components.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class DisallowComponentAttribute : Attribute, IClassAttribute
    {
        public Type[] disallowedTypes { get; }

        /// <summary>
        /// Disallow a single component.
        /// </summary>
        public DisallowComponentAttribute(Type type1)
        {
            disallowedTypes = new Type[] { type1 };
        }

        /// <summary>
        /// Disallow two components.
        /// </summary>
        public DisallowComponentAttribute(Type type1, Type type2)
        {
            disallowedTypes = new Type[] { type1, type2 };
        }

        /// <summary>
        /// Disallow three components.
        /// </summary>
        public DisallowComponentAttribute(Type type1, Type type2, Type type3)
        {
            disallowedTypes = new Type[] { type1, type2, type3 };
        }

        /// <summary>
        /// Disallow components.
        /// </summary>
        public DisallowComponentAttribute(params Type[] disallowedTypes)
        {
            this.disallowedTypes = disallowedTypes;
        }
    }
}
