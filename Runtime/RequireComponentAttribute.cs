using System;
using System.Linq;
using System.Collections.Generic;

namespace Ikonoclast.ClassAttributes
{
    /// <summary>
    /// The RequireComponent attribute automatically adds required components as dependencies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class RequireComponentAttribute : Attribute, IClassAttribute
    {
        public List<Type> required { get; }

        public RequireComponentAttribute(Type type1)
        {
            required = new List<Type>
            {
                type1
            };
        }

        public RequireComponentAttribute(Type type1, Type type2)
        {
            CheckTypes(type1, type2);

            required = new List<Type>
            {
                type1,
                type2
            };
        }

        public RequireComponentAttribute(Type type1, Type type2, Type type3)
        {
            CheckTypes(type1, type2, type3);

            required = new List<Type>
            {
                type1,
                type2,
                type3
            };
        }

        public RequireComponentAttribute(params Type[] types)
        {
            CheckTypes(types);

            required = new List<Type>(types);
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
    }
}
