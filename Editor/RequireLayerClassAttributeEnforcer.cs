using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Ikonoclast.ClassAttributes.Editor
{
    using Common;
    using Common.Editor;
    using static ClassAttributeEnforcerUtilities.UndoGroupNames;

    using Utilities = ClassAttributeEnforcerUtilities<RequireLayerAttribute>;

    [InitializeOnLoad]
    internal sealed class RequireLayerClassAttributeEnforcer : IClassAttributeEnforcer
    {
        #region Fields

        private static bool enabled = false;
        private static Assembly[] assemblies;
        private static List<Type> typesWithAttribute;

        #endregion

        #region Properties

        public static bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if (value == enabled)
                    return;

                if (value == true)
                {
                    Subscribe();
                }
                else
                {
                    Unsubscribe();
                }

                enabled = value;

                ClassAttributeEnforcerUtilities.ShouldReloadConfiguration = true;
            }
        }

        private static ISaveObject SaveObject =>
            Instance;

        private static RequireLayerClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        public RequireLayerClassAttributeEnforcer()
        {
            Utilities.Register(this);

            enabled = ClassAttributeEnforcerUtilities
                .LoadConfiguration<bool>($"{nameof(RequireLayerClassAttributeEnforcer)}.{nameof(Enabled)}");

            if (enabled)
            {
                Subscribe();
            }
        }

        static RequireLayerClassAttributeEnforcer()
        {
            Instance = new RequireLayerClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~RequireLayerClassAttributeEnforcer()
        {
            Utilities.Unregister(this);

            Unsubscribe();
        }

        #endregion

        #region Methods

        private static void Subscribe()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            ClassAttributeEnforcerEditorWindow.RequestAttributeEnforcement += OnRequestAttributeEnforcement;
        }

        private static void Unsubscribe()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            ClassAttributeEnforcerEditorWindow.RequestAttributeEnforcement -= OnRequestAttributeEnforcement;
        }

        private static void OnProjectLoaded()
        {
            Utilities.GetAllAssemblies(ref assemblies);
            Utilities.GetAllTypesWithAttribute(assemblies, ref typesWithAttribute);
            Utilities.ApplyAttributeToAllGameObjectsOfType(typesWithAttribute, ApplyAttribute);
        }

        private static void OnHierarchyChanged()
        {
            if (!Enabled)
                return;

            // Only apply the attribute if the last operation had a name containing one of these strings.
            if (!Undo.GetCurrentGroupName().ContainsAny(
                Drag,
                LayerChange,
                AddComponent,
                RemoveComponent,
                ClassAttributeEnforcer,
                InspectorChange,
                CinemachinePipelineCreated))
                return;

            Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireLayerAttribute attr))
                return;

            gameObject.layer = attr.layer;

            // Set the undo group name to avoid repeated enforcements.
            Undo.SetCurrentGroupName($"Undo $[{nameof(RequireLayerClassAttributeEnforcer)}]");
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            OnHierarchyChanged();
        }

        #endregion

        #region Event Listeners

        private static void OnRequestAttributeEnforcement()
        {
            if (Enabled)
            {
                Utilities.ApplyAttributeToAllGameObjectsOfType(typesWithAttribute, ApplyAttribute);
            }
        }

        #endregion

        #region IEditorSaveObject Implementations

        string ISaveObject.ID =>
            nameof(RequireLayerClassAttributeEnforcer);

        Map ISaveObject.Serialize()
        {
            var map = new Map(SaveObject.ID);

            map[$"{SaveObject.ID}.{nameof(Enabled)}"] = Enabled;

            return map;
        }

        void ISaveObject.Serialize(Map map, bool overwrite)
        {
            if (!overwrite && map.HasKey($"{SaveObject.ID}.{nameof(Enabled)}"))
                return;

            map[$"{SaveObject.ID}.{nameof(Enabled)}"] = Enabled;
        }

        void ISaveObject.Deserialize(Map dict)
        {
            Enabled = dict.GetRawBoolean($"{SaveObject.ID}.{nameof(Enabled)}");
        }

        bool IEditorSaveObject.Enabled
        {
            get => Enabled;
            set => Enabled = value;
        }

        void IEditorSaveObject.Reset()
        {
            Enabled = true;
        }

        #endregion

        #region IClassAttributeEnforcer Implementations

        float IClassAttributeEnforcer.ConfigurationViewHeight =>
            Utilities.BoolConfigurationHeight;

        void IClassAttributeEnforcer.OnConfigurationGUI(Vector2 size)
        {
            float offsetY = 0;

            Enabled = Utilities.MakeBoolConfiguration(size, Enabled, nameof(Enabled), ref offsetY);
        }

        #endregion
    }
}
