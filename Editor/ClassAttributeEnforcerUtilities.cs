using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections.Generic;

using UnityObject = UnityEngine.Object;

namespace Ikonoclast.ClassAttributes.Editor
{
    using Common;
    using Common.Editor;

    using static Common.Editor.PanelUtils;
    using static Common.Editor.CustomEditorHelper;

    /// <summary>
    /// This class provides useful functionality regarding <see cref="IClassAttributeEnforcer"/> classes.
    /// </summary>
    public static class ClassAttributeEnforcerUtilities
    {
        #region Inner

        public static class UndoGroupNames
        {
            public const string
                Drag = "Drag",
                AddComponent = "Add",
                RemoveComponent = "Remove",
                CreateGameObject = "Create",
                DeleteGameObject = "Delete",
                LayerChange = "Change Layer",
                InspectorChange = "Inspector",
                CinemachinePipelineCreated = "created pipeline",
                ClassAttributeEnforcer = nameof(ClassAttributeEnforcer);
        }

        #endregion

        #region Fields

        private static Map enforcerConfigurations = null;

        private static IReadOnlyList<IClassAttributeEnforcer>
            _registered = null;

        private static readonly List<IClassAttributeEnforcer>
            registered = new List<IClassAttributeEnforcer>();

        #endregion

        #region Properties

        public static bool ShouldReloadConfiguration
        {
            get;
            set;
        } = true;

        public static IReadOnlyList<IClassAttributeEnforcer> Registered =>
            _registered ?? (_registered = registered.AsReadOnly());

        #endregion

        #region Methods

        /// <summary>
        /// Save all registered enforcers to the configuration file.
        /// </summary>
        public static void SaveConfigurationFile()
        {
            if (enforcerConfigurations == null)
            {
                enforcerConfigurations = new Map("Configurations");
            }

            // Iterate over every registered enforcer and serialize its configuration.
            foreach (var item in registered)
            {
                if (!(item is IEditorSaveObject saveObject))
                    continue;

                saveObject.Serialize(enforcerConfigurations, true);
            }

            var json = JsonConvert.SerializeObject(enforcerConfigurations);

            File.WriteAllText
            (
                Path.Combine
                (
                    Directory.GetCurrentDirectory(),
                    "Packages\\com.ikonoclast.class-attributes\\Editor\\.configurations.json"
                ),
                json
            );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ShouldReloadConfiguration = false;
        }

        private static void LoadConfigurationFile()
        {
            if (enforcerConfigurations != null && !ShouldReloadConfiguration)
                return;

            string path = Path.Combine
            (
                Directory.GetCurrentDirectory(),
                "Packages\\com.ikonoclast.class-attributes\\Editor\\.configurations.json"
            );

            if (!File.Exists(path))
            {
                Debug.LogWarning("Could not find class attribute enforcer configuration file.");

                return;
            }

            var json = File.ReadAllText(path);

            if (!string.IsNullOrEmpty(json))
            {
                enforcerConfigurations = JsonConvert.DeserializeObject<Map>(json);
            }
        }

        /// <summary>
        /// Load a configuration from the configuration file.
        /// </summary>
        public static T LoadConfiguration<T>(string key)
        {
            LoadConfigurationFile();

            return enforcerConfigurations != null
                ? (T)enforcerConfigurations.GetUnsafe(key)
                : default;
        }

        public static bool? GetEnabledState<T>() where T : IClassAttribute
        {
            foreach (var item in registered)
                if (item is T)
                    return item.Enabled;

            return null;
        }

        public static void SetEnabledState<T>(bool value) where T : IClassAttribute
        {
            foreach (var item in registered)
            {
                if (item is T)
                {
                    item.Enabled = value;
                    return;
                }
            }
        }

        public static void Register(IClassAttributeEnforcer enforcer)
        {
            if (!registered.Contains(enforcer))
            {
                registered.Add(enforcer);
            }
        }

        public static void Unregister(IClassAttributeEnforcer enforcer)
        {
            registered.Remove(enforcer);
        }

        /// <summary>
        /// Attempt to destroy component on <see cref="GameObject"/> during playmode or in editor.
        /// </summary>
        public static void SafeDestroy(this GameObject gameObject, Type type)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            if (!gameObject.TryGetComponent(type, out var component))
                return;

            if (Application.isPlaying)
            {
                UnityObject.Destroy(component);
            }
            else
            {
                UnityObject.DestroyImmediate(component);
            }

            Debug.LogWarning($"Removed component of type {type} from Object {gameObject}.");
        }

        /// <summary>
        /// Determines if a string contains any of the provided strings.
        /// </summary>
        public static bool ContainsAny(this string str, params string[] strings)
        {
            if (strings == null || string.IsNullOrEmpty(str))
                return false;

            for (int i = 0; i < strings.Length; i++)
                if (str.Contains(strings[i]))
                    return true;

            return false;
        }

        #endregion
    }

    /// <summary>
    /// This class provides useful functionality specific to <see cref="IClassAttributeEnforcer"/> implementers.
    /// </summary>
    public static class ClassAttributeEnforcerUtilities<ClassAttribute>
        where ClassAttribute : IClassAttribute
    {
        public static readonly float BoolConfigurationHeight = (slh * 1.25f) + 2;

        #region Fields

        // Malleable.
        private const double bufferTime = 0.25f;

        private static readonly Dictionary<Type, double>
            enforcerBuffer = new Dictionary<Type, double>();

        #endregion

        #region Methods

        /// ---
        /// Generate UI for editing a boolean config.
        public static bool MakeBoolConfiguration(Vector2 size, bool value, string configName, ref float offsetY)
        {
            var configRect = new Rect
            {
                x = 2,
                y = offsetY + 2,
                width = size.x - 4,
                height = slh * 1.25f
            };

            offsetY += configRect.height + 2;

            MakeBox(configRect);

            configRect.x += 5;
            configRect.y += 1;

            GUI.Label(configRect, configName, EditorStyles.boldLabel);

            configRect.y += 1;

            VGUILine(configRect, configRect.height - 3f, configRect.width * 0.9f);

            configRect.x = configRect.width - slh;
            configRect.y += 1;
            configRect.width = slh;
            configRect.height = slh;

            return GUI.Toggle(configRect, value, GUIContent.none);
        }

        public static void Register(IClassAttributeEnforcer enforcer) =>
            ClassAttributeEnforcerUtilities.Register(enforcer);

        public static void Unregister(IClassAttributeEnforcer enforcer) =>
            ClassAttributeEnforcerUtilities.Unregister(enforcer);

        public static void GetAllAssemblies(ref Assembly[] assemblies)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        public static void GetAllTypesWithAttribute(Assembly[] assemblies, ref List<Type> typesWithAttribute)
        {
            if (assemblies == null)
                return;

            typesWithAttribute = new List<Type>();

            foreach (var assembly in assemblies)
            {
                typesWithAttribute.AddRange(assembly.GetTypes()?.Where(t => t.IsDefined(typeof(ClassAttribute))));
            }
        }

        public static void ApplyAttributeToSelectedGameObject(List<Type> typesWithAttribute, Action<GameObject, Attribute> ApplyAttribute)
        {
            if (typesWithAttribute == null)
                return;

            if (ApplyAttribute == null)
                return;

            if (enforcerBuffer.TryGetValue(typeof(ClassAttribute), out var lastEnforcedTime))
                if ((EditorApplication.timeSinceStartup - lastEnforcedTime) < bufferTime)
                    return;

            var gameObject = Selection.activeGameObject;

            if (gameObject == null)
                return;

            // On the active game object, find all components of type
            // and, for each one that has the specified ClassAttribute, apply the attribute.
            foreach (var type in typesWithAttribute)
            {
                if (gameObject.GetComponent(type))
                {
                    var attrs = Attribute.GetCustomAttributes(type, typeof(ClassAttribute));

                    if (attrs == null)
                        continue;

                    foreach (var attr in attrs)
                    {
                        ApplyAttribute.Invoke(gameObject, attr);
                    }
                }
            }

            // Update the buffer.
            enforcerBuffer[typeof(ClassAttribute)] = EditorApplication.timeSinceStartup;
        }

        public static void ApplyAttributeToAllGameObjectsOfType(List<Type> typesWithAttribute, Action<GameObject, Attribute> ApplyAttribute)
        {
            if (typesWithAttribute == null)
                return;

            if (ApplyAttribute == null)
                return;

            if (enforcerBuffer.TryGetValue(typeof(ClassAttribute), out var lastEnforcedTime))
                if ((EditorApplication.timeSinceStartup - lastEnforcedTime) < bufferTime)
                    return;

            // Similar logic as with the above method except across all objects of each type in the open scene(s).
            foreach (var type in typesWithAttribute)
            {
                if (type.IsGenericTypeDefinition)
                    continue;

                var objects = UnityObject.FindObjectsOfType(type, true);

                if (objects == null)
                    continue;

                foreach (var obj in objects)
                {
                    if (obj is Component component)
                    {
                        var attrs = Attribute.GetCustomAttributes(type, typeof(ClassAttribute));

                        if (attrs == null)
                            continue;

                        foreach (var attr in attrs)
                        {
                            ApplyAttribute.Invoke(component.gameObject, attr);
                        }
                    }
                }
            }

            enforcerBuffer[typeof(ClassAttribute)] = EditorApplication.timeSinceStartup;
        }

        #endregion
    }
}