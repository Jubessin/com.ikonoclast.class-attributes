using System;
using System.Collections.Generic;
using System.Linq;

namespace Ikonoclast.ClassAttributes
{
    /// <summary>
    /// The RequireChildComponent attribute automatically adds required child components as dependencies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class RequireChildComponentAttribute : Attribute, IClassAttribute
    {
        public Dictionary<Type, string> required { get; }

        #region Constructors

        /// <summary>
        /// Require a single child component.
        /// </summary>
        public RequireChildComponentAttribute(Type type1)
        {
            required = new Dictionary<Type, string>
            {
                { type1, GetDefaultNaming(type1) }
            };
        }

        /// <summary>
        /// Require a single, named child component.
        /// </summary>
        public RequireChildComponentAttribute(Type type1, string name1)
        {
            required = new Dictionary<Type, string> { { type1, name1 } };
        }

        /// <summary>
        /// Require two child components.
        /// </summary>
        public RequireChildComponentAttribute(Type type1, Type type2)
        {
            CheckTypes(type1, type2);

            required = new Dictionary<Type, string>
            {
                { type1, GetDefaultNaming(type1) },
                { type2, GetDefaultNaming(type2) },
            };
        }

        /// <summary>
        /// Require two, named child components.
        /// </summary>
        public RequireChildComponentAttribute(Type type1, Type type2, string name1, string name2)
        {
            CheckTypes(type1, type2);

            required = new Dictionary<Type, string>
            {
                { type1, name1 },
                { type2, name2 },
            };
        }

        /// <summary>
        /// Require three child components.
        /// </summary>
        public RequireChildComponentAttribute(Type type1, Type type2, Type type3)
        {
            CheckTypes(type1, type2, type3);

            required = new Dictionary<Type, string>
            {
                { type1, GetDefaultNaming(type1) },
                { type2, GetDefaultNaming(type2) },
                { type3, GetDefaultNaming(type3) },
            };
        }

        /// <summary>
        /// Require three, named child components.
        /// </summary>
        public RequireChildComponentAttribute(Type type1, Type type2, Type type3, string name1, string name2, string name3)
        {
            CheckTypes(type1, type2, type3);

            required = new Dictionary<Type, string>
            {
                { type1, name1 },
                { type2, name2 },
                { type3, name3 },
            };
        }

        /// <summary>
        /// Require child components.
        /// </summary>
        public RequireChildComponentAttribute(params Type[] requiredTypes)
        {
            CheckTypes(requiredTypes);

            required = new Dictionary<Type, string>();

            foreach (var type in requiredTypes)
            {
                required[type] = GetDefaultNaming(type);
            }
        }

        #endregion

        #region Methods

        private string GetDefaultNaming(Type type)
        {
            return $"RequireChildComponent_{type.Name}";
        }

        private void CheckTypes(Type type1, Type type2)
        {
            if (type1 == type2)
                throw new Exception($"{nameof(RequireChildComponentAttribute)} types must be unique.");
        }
        private void CheckTypes(Type type1, Type type2, Type type3)
        {
            if (type1 == type2)
                throw new Exception($"{nameof(RequireChildComponentAttribute)} types must be unique.");

            if (type2 == type3)
                throw new Exception($"{nameof(RequireChildComponentAttribute)} types must be unique.");

            if (type1 == type3)
                throw new Exception($"{nameof(RequireChildComponentAttribute)} types must be unique.");
        }
        private void CheckTypes(params Type[] types)
        {
            if (types.Distinct().Count() != types.Count())
                throw new Exception($"{nameof(RequireChildComponentAttribute)} types must be unique.");
        }

        #endregion
    }
}
