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

    using Utilities = ClassAttributeEnforcerUtilities<RequireLayerAttribute>;

    [InitializeOnLoad]
    internal sealed class RequireLayerClassAttributeEnforcer : IClassAttributeEnforcer
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
                { nameof(enabled), $"{nameof(RequireLayerClassAttributeEnforcer)}.{nameof(Enabled)}" },
                { nameof(enforceOnSceneChange), $"{nameof(RequireLayerClassAttributeEnforcer)}.Enforce On Scene Change" },
                { nameof(enforceOnSelectionChange), $"{nameof(RequireLayerClassAttributeEnforcer)}.Enforce On Selection Change" },
            };

        #endregion

        #region Properties

        public string ID =>
            nameof(RequireLayerClassAttributeEnforcer);

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

            LoadConfigurations();

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
                LayerChange,
                AddComponent,
                InspectorChange,
                RemoveComponent,
                ClassAttributeEnforcer,
                CinemachinePipelineCreated))
                return;

            Utilities.ApplyAttributeToSelectedGameObject(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireLayerAttribute attr))
                return;

            if (gameObject.layer == LayerMask.NameToLayer(attr.layerName))
                return;

            var asset = AssetDatabase.LoadAssetAtPath<UnityObject>("ProjectSettings/TagManager.asset");

            var tagManagerObject = new SerializedObject(asset);

            var layers = tagManagerObject.FindProperty("layers");

            if (layers == null)
            {
                Debug.LogError("Could not find 'layers' property in TagManager.");

                return;
            }

            if (!layers.isArray)
            {
                Debug.LogError("'layers' property in TagManager is not an array.");

                return;
            }

            int? idx = null;
            var prop = layers.GetArrayElementAtIndex(0);

            for (int i = 0; prop.propertyType == SerializedPropertyType.String; ++i)
            {
                if (prop.stringValue == attr.layerName)
                {
                    gameObject.layer = LayerMask.NameToLayer(attr.layerName);

                    // Set the undo group name to avoid repeated enforcements.
                    Undo.SetCurrentGroupName($"Undo $[{nameof(RequireLayerClassAttributeEnforcer)}]");

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
                prop = layers.GetArrayElementAtIndex(idx.GetValueOrDefault());

                if (string.IsNullOrWhiteSpace(prop.stringValue))
                {
                    prop.stringValue = attr.layerName;

                    tagManagerObject.ApplyModifiedPropertiesWithoutUndo();

                    gameObject.layer = LayerMask.NameToLayer(attr.layerName);

                    Debug.Log($"{attr.layerName} was created and applied to {gameObject}.");

                    // Set the undo group name to avoid repeated enforcements.
                    Undo.SetCurrentGroupName($"Undo $[{nameof(RequireLayerClassAttributeEnforcer)}]");

                    return;
                }

                Debug.LogWarning(
                    $"{attr.layerName} could not be created or applied to {gameObject}. " +
                        $"There may be no available layers [0...31].");
            }

            Debug.LogWarning(
                $"{attr.layerName} is not defined, and was not created or applied to {gameObject}.");
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
