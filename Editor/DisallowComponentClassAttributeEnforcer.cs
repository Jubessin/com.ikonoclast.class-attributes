using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Ikonoclast.ClassAttributes.Editor
{
    using static ClassAttributeEnforcerUtilities.UndoGroupNames;

    using Utilities = ClassAttributeEnforcerUtilities<DisallowComponentAttribute>;

    [InitializeOnLoad]
    internal sealed class DisallowComponentClassAttributeEnforcer : IClassAttributeEnforcer
    {
        #region Fields

        private static Assembly[] assemblies;
        private static List<Type> typesWithAttribute;

        #endregion

        #region Properties

        private static DisallowComponentClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        static DisallowComponentClassAttributeEnforcer()
        {
            Instance = new DisallowComponentClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~DisallowComponentClassAttributeEnforcer()
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
            if (!Undo.GetCurrentGroupName().ContainsAny(AddComponent, RemoveComponent, ClassAttributeEnforcer))
                return;

            Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is DisallowComponentAttribute attr))
                return;

            var disallowedTypes = attr.disallowedTypes;

            foreach (var type in disallowedTypes)
            {
                gameObject.SafeDestroy(type);
            }

            Undo.SetCurrentGroupName($"Undo $[{nameof(DisallowComponentClassAttributeEnforcer)}]");
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            OnHierarchyChanged();
        }

        #endregion
    }
}
