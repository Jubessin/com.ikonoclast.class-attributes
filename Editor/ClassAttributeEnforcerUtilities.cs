using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Ikonoclast.ClassAttributes.Editor
{
    using UnityObject = UnityEngine.Object;

    public static class ClassAttributeEnforcerUtilities
    {
        public static class UndoGroupNames
        {
            public const string
                AddComponent = "Add",
                RemoveComponent = "Remove",
                CreateGameObject = "Create",
                DeleteGameObject = "Delete",
                LayerChange = "Change Layer",
                InspectorChange = "Inspector",
                CinemachinePipelineCreated = "created pipeline",
                ClassAttributeEnforcer = nameof(ClassAttributeEnforcer);
        }

        public static void SafeDestroy(this GameObject gameObject, Type type)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            if (!gameObject.TryGetComponent(type, out var component))
                return;

            if (Application.isPlaying)
            {
                UnityObject.Destroy(component);
            }
            else
            {
                UnityObject.DestroyImmediate(component);
            }

            Debug.LogWarning($"Removed component of type {type} from Object {gameObject}.");
        }

        public static bool ContainsAny(this string str, params string[] strings)
        {
            if (strings == null || string.IsNullOrEmpty(str))
                return false;

            for (int i = 0; i < strings.Length; i++)
                if (str.Contains(strings[i]))
                    return true;

            return false;
        }
    }

    public static class ClassAttributeEnforcerUtilities<ClassAttribute>
        where ClassAttribute : IClassAttribute
    {
        // Malleable.
        private const double bufferTime = 0.25f;

        private static readonly Dictionary<Type, double>
            enforcerBuffer = new Dictionary<Type, double>();

        public static void GetAllAssemblies(ref Assembly[] assemblies)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        public static void GetAllTypesWithAttribute(Assembly[] assemblies, ref List<Type> typesWithAttribute)
        {
            if (assemblies == null)
                return;

            typesWithAttribute = new List<Type>();

            foreach (var assembly in assemblies)
            {
                typesWithAttribute.AddRange(assembly.GetTypes()?.Where(t => t.IsDefined(typeof(ClassAttribute))));
            }
        }

        public static void ApplyAttributeToSelectedGameObject(List<Type> typesWithAttribute, Action<GameObject, Attribute> ApplyAttribute)
        {
            if (typesWithAttribute == null)
                return;

            if (ApplyAttribute == null)
                return;

            if (enforcerBuffer.TryGetValue(typeof(ClassAttribute), out var lastEnforcedTime))
                if ((EditorApplication.timeSinceStartup - lastEnforcedTime) < bufferTime)
                    return;

            var gameObject = Selection.activeGameObject;

            if (gameObject == null)
                return;

            foreach (var type in typesWithAttribute)
            {
                if (gameObject.GetComponent(type))
                {
                    var attrs = Attribute.GetCustomAttributes(type, typeof(ClassAttribute));

                    if (attrs == null)
                        continue;

                    foreach (var attr in attrs)
                    {
                        ApplyAttribute.Invoke(gameObject, attr);
                    }
                }
            }

            enforcerBuffer[typeof(ClassAttribute)] = EditorApplication.timeSinceStartup;
        }

        public static void ApplyAttributeToAllGameObjectsOfType(List<Type> typesWithAttribute, Action<GameObject, Attribute> ApplyAttribute)
        {
            if (typesWithAttribute == null)
                return;

            if (ApplyAttribute == null)
                return;

            if (enforcerBuffer.TryGetValue(typeof(ClassAttribute), out var lastEnforcedTime))
                if ((EditorApplication.timeSinceStartup - lastEnforcedTime) < bufferTime)
                    return;

            foreach (var type in typesWithAttribute)
            {
                if (type.IsGenericTypeDefinition)
                    continue;

                var objects = UnityObject.FindObjectsOfType(type, true);

                if (objects == null)
                    continue;

                foreach (var obj in objects)
                {
                    if (obj is Component component)
                    {
                        var attrs = Attribute.GetCustomAttributes(type, typeof(ClassAttribute));

                        if (attrs == null)
                            continue;

                        foreach (var attr in attrs)
                        {
                            ApplyAttribute.Invoke(component.gameObject, attr);
                        }
                    }
                }
            }

            enforcerBuffer[typeof(ClassAttribute)] = EditorApplication.timeSinceStartup;
        }
    }
}