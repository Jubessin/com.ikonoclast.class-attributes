using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Ikonoclast.ClassAttributes.Editor
{
    using static ClassAttributeEnforcerUtilities.UndoGroupNames;

    using Utilities = ClassAttributeEnforcerUtilities<RequireLayerAttribute>;

    [InitializeOnLoad]
    internal sealed class RequireLayerClassAttributeEnforcer : IClassAttributeEnforcer
    {
        #region Fields

        private static Assembly[] assemblies;
        private static List<Type> typesWithAttribute;

        #endregion

        #region Properties

        private static RequireLayerClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        static RequireLayerClassAttributeEnforcer()
        {
            Instance = new RequireLayerClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~RequireLayerClassAttributeEnforcer()
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
            if (!Undo.GetCurrentGroupName().ContainsAny(LayerChange, AddComponent, RemoveComponent, ClassAttributeEnforcer, InspectorChange, CinemachinePipelineCreated))
                return;

            Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireLayerAttribute attr))
                return;

            gameObject.layer = attr.layer;

            Undo.SetCurrentGroupName($"Undo $[{nameof(RequireLayerClassAttributeEnforcer)}]");
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            OnHierarchyChanged();
        }

        #endregion
    }
}
