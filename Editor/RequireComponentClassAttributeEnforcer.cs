using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Ikonoclast.ClassAttributes.Editor
{
    using static ClassAttributeEnforcerUtilities.UndoGroupNames;

    using Utilities = ClassAttributeEnforcerUtilities<RequireComponentAttribute>;

    /* Limitations:
     * 
     * Does not work with interfaces/generics/abstracts.
    */
    [InitializeOnLoad]
    internal sealed class RequireComponentClassAttributeEnforcer : IClassAttributeEnforcer
    {
        #region Fields

        private static Assembly[] assemblies;
        private static List<Type> typesWithAttribute;

        #endregion

        #region Properties

        private static RequireComponentClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        static RequireComponentClassAttributeEnforcer()
        {
            Instance = new RequireComponentClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~RequireComponentClassAttributeEnforcer()
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
            if (!Undo.GetCurrentGroupName().ContainsAny(AddComponent, RemoveComponent, InspectorChange, ClassAttributeEnforcer))
                return;

            Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireComponentAttribute attr))
                return;

            var required = attr.required;

            foreach (var type in required)
            {
                if (!gameObject.TryGetComponent(type, out var _))
                {
                    if (type.IsInterface || type.IsAbstract || type.IsGenericType)
                    {
                        Debug.LogWarning(
                            $"Could not add required component of type {type} to Object {gameObject}.");
                    }
                    else
                    {
                        gameObject.AddComponent(type);
                    }
                }
            }

            Undo.SetCurrentGroupName($"Undo $[{nameof(RequireComponentClassAttributeEnforcer)}]");
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            OnHierarchyChanged();
        }

        #endregion
    }
}
