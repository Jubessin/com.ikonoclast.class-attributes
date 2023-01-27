using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Ikonoclast.ClassAttributes.Editor
{
    using static ClassAttributeEnforcerUtilities.UndoGroupNames;

    using Utilities = ClassAttributeEnforcerUtilities<RequireChildComponentAttribute>;

    /// ---
    /// This enforcer is a bit more expensive than others, 
    /// as it must iterate game objects with each applicable hierarchy change.
    [InitializeOnLoad]
    internal sealed class RequireChildComponentClassAttributeEnforcer : IClassAttributeEnforcer
    {
        #region Fields

        private static Assembly[] assemblies;
        private static List<Type> typesWithAttribute;

        #endregion

        #region Properties

        private static RequireChildComponentClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        static RequireChildComponentClassAttributeEnforcer()
        {
            Instance = new RequireChildComponentClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~RequireChildComponentClassAttributeEnforcer()
        {
            Unsubscribe();
        }

        #endregion

        #region Methods

        private static void Subscribe()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void Unsubscribe()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private static void OnProjectLoaded()
        {
            Utilities.GetAllAssemblies(ref assemblies);
            Utilities.GetAllTypesWithAttribute(assemblies, ref typesWithAttribute);
            Utilities.ApplyAttributeToAllGameObjectsOfType(typesWithAttribute, ApplyAttribute);
        }

        private static void OnHierarchyChanged()
        {
            if (!Undo.GetCurrentGroupName().ContainsAny(AddComponent, RemoveComponent, DeleteGameObject, ClassAttributeEnforcer))
                return;

            Utilities.ApplyAttributeToAllGameObjectsOfType(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireChildComponentAttribute attr))
                return;

            void AddRequiredComponent(KeyValuePair<Type, string> kvp)
            {
                if (Attribute.GetCustomAttribute(kvp.Key, typeof(RequireNameAttribute)) != null)
                    throw new NotSupportedException($"Conflicting attributes on {kvp.Key.FullName}. " +
                                $"{nameof(RequireChildComponentAttribute)} cannot be used with {nameof(RequireNameAttribute)}.");

                string childName = kvp.Value;

                var parent = gameObject.transform;

                foreach (Transform child in parent)
                {
                    if (child.name == childName)
                    {
                        child.gameObject.AddComponent(kvp.Key);

                        return;
                    }
                }

                var newChild = new GameObject(childName);

                newChild.tag = parent.tag;
                newChild.AddComponent(kvp.Key);
                newChild.transform.parent = parent;
            }

            var required = attr.required;

            foreach (var kvp in required)
            {
                var components = gameObject.GetComponentsInChildren(kvp.Key, true);

                if (components == null || components.Length == 0)
                {
                    AddRequiredComponent(kvp);
                }
                else
                {
                    bool foundChild = false;

                    foreach (var comp in components)
                    {
                        // Check that the component is on a child gameObject.
                        if (comp.gameObject != gameObject)  // NullRef with IActorVision attr on AIActor
                        {
                            foundChild = true;

                            break;
                        }
                    }

                    if (!foundChild)
                    {
                        AddRequiredComponent(kvp);
                    }
                }
            }

            Undo.SetCurrentGroupName($"Undo $[{nameof(RequireChildComponentClassAttributeEnforcer)}]");
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            OnHierarchyChanged();
        }

        #endregion
    }
}
