using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

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

        private static bool
            enforceOnSceneChange,
            enforceOnSelectionChange;

        private static readonly Dictionary<string, string>
            propertyKeys = new Dictionary<string, string>()
            {
                { nameof(enabled), $"{nameof(DisallowComponentClassAttributeEnforcer)}.{nameof(Enabled)}" },
                { nameof(enforceOnSceneChange), $"{nameof(DisallowComponentClassAttributeEnforcer)}.Enforce On Scene Change" },
                { nameof(enforceOnSelectionChange), $"{nameof(DisallowComponentClassAttributeEnforcer)}.Enforce On Selection Change" },
            };

        #endregion

        #region Properties

        public string ID =>
            nameof(DisallowComponentClassAttributeEnforcer);

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

            LoadConfigurations();

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
            EditorSceneManager.sceneOpened += OnSceneOpened;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            ClassAttributeEnforcerEditorWindow.RequestAttributeEnforcement += OnRequestAttributeEnforcement;
        }

        private static void Unsubscribe()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            ClassAttributeEnforcerEditorWindow.RequestAttributeEnforcement -= OnRequestAttributeEnforcement;
        }

        private static void LoadConfigurations()
        {
            enabled = ClassAttributeEnforcerUtilities
                .LoadConfiguration<bool>(propertyKeys[nameof(enabled)]);

            enforceOnSceneChange = ClassAttributeEnforcerUtilities
                .LoadConfiguration<bool>(propertyKeys[nameof(enforceOnSceneChange)]);

            enforceOnSelectionChange = ClassAttributeEnforcerUtilities
                .LoadConfiguration<bool>(propertyKeys[nameof(enforceOnSelectionChange)]);
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
                AddComponent,
                RemoveComponent,
                ClassAttributeEnforcer))
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

        #endregion

        #region Event Listeners

        private static void OnSelectionChanged()
        {
            if (Enabled && enforceOnSelectionChange)
            {
                Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
            }
        }

        private static void OnRequestAttributeEnforcement()
        {
            if (Enabled)
            {
                Utilities.ApplyAttributeToAllGameObjectsOfType(typesWithAttribute, ApplyAttribute);
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (enforceOnSceneChange)
            {
                OnHierarchyChanged();
            }
        }

        #endregion

        #region IEditorSaveObject Implementations

        Map ISaveObject.Serialize()
        {
            var map = new Map(SaveObject.ID);

            map[propertyKeys[nameof(enabled)]] = Enabled;
            map[propertyKeys[nameof(enforceOnSceneChange)]] = enforceOnSceneChange;
            map[propertyKeys[nameof(enforceOnSelectionChange)]] = enforceOnSelectionChange;

            return map;
        }

        void ISaveObject.Serialize(Map map, bool overwrite)
        {
            if (overwrite)
            {
                map[propertyKeys[nameof(enabled)]] = Enabled;
                map[propertyKeys[nameof(enforceOnSceneChange)]] = enforceOnSceneChange;
                map[propertyKeys[nameof(enforceOnSelectionChange)]] = enforceOnSelectionChange;
            }
            else
            {
                if (!map.HasKey(propertyKeys[nameof(enabled)]))
                {
                    map[propertyKeys[nameof(enabled)]] = Enabled;
                }

                if (!map.HasKey(propertyKeys[nameof(enforceOnSceneChange)]))
                {
                    map[propertyKeys[nameof(enforceOnSceneChange)]] = enforceOnSceneChange;
                }

                if (!map.HasKey(propertyKeys[nameof(enforceOnSelectionChange)]))
                {
                    map[propertyKeys[nameof(enforceOnSelectionChange)]] = enforceOnSelectionChange;
                }
            }
        }

        void ISaveObject.Deserialize(Map dict)
        {
            Enabled = dict.GetRawBoolean(propertyKeys[nameof(Enabled)]);
            enforceOnSceneChange = dict.GetRawBoolean(propertyKeys[nameof(enforceOnSceneChange)]);
            enforceOnSelectionChange = dict.GetRawBoolean(propertyKeys[nameof(enforceOnSelectionChange)]);
        }

        bool IEditorSaveObject.Enabled
        {
            get => Enabled;
            set => Enabled = value;
        }

        void IEditorSaveObject.Reset()
        {
            Enabled = true;
            enforceOnSceneChange = true;
            enforceOnSelectionChange = true;
        }

        #endregion

        #region IClassAttributeEnforcer Implementations

        float IClassAttributeEnforcer.ConfigurationViewHeight =>
            Utilities.BoolConfigurationHeight * 3;

        void IClassAttributeEnforcer.OnConfigurationGUI(Vector2 size)
        {
            float offsetY = 0;

            Enabled = Utilities.MakeBoolConfiguration
            (
                size,
                Enabled,
                nameof(Enabled),
                ref offsetY
            );

            EditorGUI.BeginDisabledGroup(!Enabled);

            enforceOnSceneChange = Utilities.MakeBoolConfiguration
            (
                size,
                enforceOnSceneChange,
                "Enforce On Scene Change",
                ref offsetY
            );

            enforceOnSelectionChange = Utilities.MakeBoolConfiguration
            (
                size,
                enforceOnSelectionChange,
                "Enforce On Selection Change",
                ref offsetY
            );

            EditorGUI.EndDisabledGroup();
        }

        #endregion
    }
}
