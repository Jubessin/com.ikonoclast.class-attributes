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

    using Utilities = ClassAttributeEnforcerUtilities<RequireNameAttribute>;

    [InitializeOnLoad]
    internal sealed class RequireNameClassAttributeEnforcer : IClassAttributeEnforcer
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

        private static RequireNameClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        public RequireNameClassAttributeEnforcer()
        {
            Utilities.Register(this);

            enabled = ClassAttributeEnforcerUtilities
                .LoadConfiguration<bool>($"{nameof(RequireNameClassAttributeEnforcer)}.{nameof(Enabled)}");

            if (enabled)
            {
                Subscribe();
            }
        }

        static RequireNameClassAttributeEnforcer()
        {
            Instance = new RequireNameClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~RequireNameClassAttributeEnforcer()
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
            if (!Undo.GetCurrentGroupName().ContainsAny(Drag, CreateGameObject, InspectorChange, AddComponent, ClassAttributeEnforcer))
                return;

            Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireNameAttribute attr))
                return;

            gameObject.name = attr.name;

            // Set the undo group name to avoid repeated enforcements.
            Undo.SetCurrentGroupName($"Undo $[{nameof(RequireNameClassAttributeEnforcer)}]");
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
            nameof(RequireNameClassAttributeEnforcer);

        Map ISaveObject.Serialize()
        {
            var map = new Map(SaveObject.ID);

            map[nameof(Enabled)] = Enabled;

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