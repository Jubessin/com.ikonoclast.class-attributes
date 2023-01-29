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

    using Utilities = ClassAttributeEnforcerUtilities<DisallowComponentAttribute>;

    [InitializeOnLoad]
    internal sealed class DisallowComponentClassAttributeEnforcer : IClassAttributeEnforcer
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

        private static DisallowComponentClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        public DisallowComponentClassAttributeEnforcer()
        {
            Utilities.Register(this);

            enabled = ClassAttributeEnforcerUtilities
                .LoadConfiguration<bool>($"{nameof(DisallowComponentClassAttributeEnforcer)}.{nameof(Enabled)}");

            if (enabled)
            {
                Subscribe();
            }
        }

        static DisallowComponentClassAttributeEnforcer()
        {
            Instance = new DisallowComponentClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~DisallowComponentClassAttributeEnforcer()
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
            if (!Undo.GetCurrentGroupName().ContainsAny(Drag, AddComponent, RemoveComponent, ClassAttributeEnforcer))
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

            // Set the undo group name to avoid repeated enforcements.
            Undo.SetCurrentGroupName($"Undo $[{nameof(DisallowComponentClassAttributeEnforcer)}]");
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
            nameof(DisallowComponentClassAttributeEnforcer);

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

        void ISaveObject.Deserialize(Map map)
        {
            Enabled = map.GetRawBoolean($"{SaveObject.ID}.{nameof(Enabled)}");
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
