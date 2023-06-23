using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using UnityObject = UnityEngine.Object;

namespace Ikonoclast.ClassAttributes.Editor
{
    using Common;
    using Common.Editor;

    using static ClassAttributeEnforcerUtilities.UndoGroupNames;

    using Utilities = ClassAttributeEnforcerUtilities<RequireTagAttribute>;

    [InitializeOnLoad]
    internal sealed class RequireTagClassAttributeEnforcer : IClassAttributeEnforcer
    {
        #region Fields

        private static bool isSubscribed;
        private static bool enabled = false;
        private static Assembly[] assemblies;
        private static List<Type> typesWithAttribute;

        private static bool
            enforceOnSceneChange,
            enforceOnSelectionChange;

        private static readonly Dictionary<string, string>
            propertyKeys = new Dictionary<string, string>()
            {
                { nameof(enabled), $"{nameof(RequireTagClassAttributeEnforcer)}.{nameof(Enabled)}" },
                { nameof(enforceOnSceneChange), $"{nameof(RequireTagClassAttributeEnforcer)}.Enforce On Scene Change" },
                { nameof(enforceOnSelectionChange), $"{nameof(RequireTagClassAttributeEnforcer)}.Enforce On Selection Change" },
            };

        #endregion

        #region Properties

        public string ID =>
            nameof(RequireTagClassAttributeEnforcer);

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

        private static RequireTagClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        public RequireTagClassAttributeEnforcer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            Utilities.Register(this);

            LoadConfigurations();

            if (enabled)
            {
                Subscribe();
            }
        }

        static RequireTagClassAttributeEnforcer()
        {
            Instance = new RequireTagClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~RequireTagClassAttributeEnforcer()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            Utilities.Unregister(this);

            Unsubscribe();
        }

        #endregion

        #region Methods

        private static void Subscribe()
        {
            if (isSubscribed)
                return;

            EditorSceneManager.sceneOpened += OnSceneOpened;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            ClassAttributeEnforcerEditorWindow.RequestAttributeEnforcement += OnRequestAttributeEnforcement;

            isSubscribed = true;
        }

        private static void Unsubscribe()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            ClassAttributeEnforcerEditorWindow.RequestAttributeEnforcement -= OnRequestAttributeEnforcement;

            isSubscribed = false;
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
                LayerChange,
                AddComponent,
                InspectorChange,
                RemoveComponent,
                ClassAttributeEnforcer,
                CinemachinePipelineCreated))
                return;

            Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
        }

        private static bool IsBuiltIn(string tag) =>
            tag == "Untagged" ||
            tag == "Respawn" ||
            tag == "Finish" ||
            tag == "EditorOnly" ||
            tag == "MainCamera" ||
            tag == "Player" ||
            tag == "GameController";

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireTagAttribute attr))
                return;

            if (gameObject.tag == attr.tag)
                return;

            if (IsBuiltIn(attr.tag))
            {
                gameObject.tag = attr.tag;

                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityObject>("ProjectSettings/TagManager.asset");

            var tagManagerObject = new SerializedObject(asset);

            var tags = tagManagerObject.FindProperty("tags");

            if (tags == null)
            {
                Debug.LogError("Could not find 'tags' property in TagManager.");

                return;
            }

            if (!tags.isArray)
            {
                Debug.LogError("'tags' property in TagManager is not an array.");

                return;
            }

            int? idx = null;
            var prop = tags.GetArrayElementAtIndex(0);

            for (int i = 0; prop.propertyType == SerializedPropertyType.String; ++i)
            {
                if (prop.stringValue == attr.tag)
                {
                    gameObject.tag = attr.tag;

                    // Set the undo group name to avoid repeated enforcements.
                    Undo.SetCurrentGroupName($"Undo $[{nameof(RequireTagClassAttributeEnforcer)}]");

                    return;
                }

                if (!idx.HasValue && string.IsNullOrWhiteSpace(prop.stringValue))
                {
                    idx = i;
                }

                prop.Next(false);
            }

            if (attr.createIfNotDefined)
            {
                idx = idx.GetValueOrDefault();

                if (!string.IsNullOrWhiteSpace(tags.GetArrayElementAtIndex(idx.Value).stringValue))
                {
                    idx = tags.arraySize;

                    tags.InsertArrayElementAtIndex(idx.Value);
                }

                tags.GetArrayElementAtIndex(idx.Value).stringValue = attr.tag;

                tagManagerObject.ApplyModifiedPropertiesWithoutUndo();

                gameObject.tag = attr.tag;

                Debug.Log($"{attr.tag} was created and applied to {gameObject}.");

                // Set the undo group name to avoid repeated enforcements.
                Undo.SetCurrentGroupName($"Undo $[{nameof(RequireTagClassAttributeEnforcer)}]");

                return;
            }

            Debug.LogWarning(
                $"{attr.tag} is not defined, and was not created or applied to {gameObject}.");
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

        private static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredEditMode: Subscribe(); break;
                case PlayModeStateChange.EnteredPlayMode: Unsubscribe(); break;
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
