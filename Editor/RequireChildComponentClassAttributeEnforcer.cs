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

    using Utilities = ClassAttributeEnforcerUtilities<RequireChildComponentAttribute>;

    /// ---
    /// This enforcer is a bit more expensive than others, 
    /// as it must iterate game objects with each applicable hierarchy change.
    [InitializeOnLoad]
    internal sealed class RequireChildComponentClassAttributeEnforcer : IClassAttributeEnforcer
    {
        #region Fields

        private static bool enabled = false;
        private static Assembly[] assemblies;
        private static List<Type> typesWithAttribute;

        #endregion

        #region Properties

        public string ID =>
            nameof(RequireChildComponentClassAttributeEnforcer);

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

        private static RequireChildComponentClassAttributeEnforcer Instance
        {
            get;
        }

        #endregion

        #region Constructors

        public RequireChildComponentClassAttributeEnforcer()
        {
            Utilities.Register(this);

            enabled = ClassAttributeEnforcerUtilities
                .LoadConfiguration<bool>($"{nameof(RequireChildComponentClassAttributeEnforcer)}.{nameof(Enabled)}");

            if (enabled)
            {
                Subscribe();
            }
        }

        static RequireChildComponentClassAttributeEnforcer()
        {
            Instance = new RequireChildComponentClassAttributeEnforcer();

            Subscribe();

            OnProjectLoaded();
        }

        ~RequireChildComponentClassAttributeEnforcer()
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
            if (!Undo.GetCurrentGroupName().ContainsAny(Drag, AddComponent, RemoveComponent, DeleteGameObject, ClassAttributeEnforcer))
                return;

            Utilities.ApplyAttributeToAllGameObjectsOfType(typesWithAttribute, ApplyAttribute);
        }

        private static void ApplyAttribute(GameObject gameObject, Attribute attribute)
        {
            if (!(attribute is RequireChildComponentAttribute attr))
                return;

            void AddRequiredComponent(KeyValuePair<Type, string> kvp)
            {
                // Verify that the child being added does not have the RequireNameAttribute.
                if (Attribute.GetCustomAttribute(kvp.Key, typeof(RequireNameAttribute)) != null)
                    throw new NotSupportedException($"Conflicting attributes on {kvp.Key.FullName}. " +
                        $"{nameof(RequireChildComponentAttribute)} cannot be used with {nameof(RequireNameAttribute)}.");

                var childName = kvp.Value;

                var parent = gameObject.transform;

                // Check every child of the game object, and
                // add the required component if one with a matching name is found without the component.
                foreach (Transform child in parent)
                {
                    if (child.name == childName)
                    {
                        child.gameObject.AddComponent(kvp.Key);

                        return;
                    }
                }

                // Create a new child with the specified (or default) name, add the component, and assign its parent.
                var newChild = new GameObject(childName);

                newChild.tag = parent.tag;
                newChild.AddComponent(kvp.Key);
                newChild.transform.SetParent(parent);
            }

            var required = attr.required;

            foreach (var kvp in required)
            {
                var components = gameObject.GetComponentsInChildren(kvp.Key, true);

                if (components == null || components.Length == 0)
                {
                    AddRequiredComponent(kvp);
                }
                else
                {
                    bool foundChild = false;

                    foreach (var comp in components)
                    {
                        // Check that the component is on a child gameObject.
                        if (comp.gameObject != gameObject)  // TODO: Investigate null reference with interfaces
                        {
                            foundChild = true;

                            break;
                        }
                    }

                    if (!foundChild)
                    {
                        AddRequiredComponent(kvp);
                    }
                }
            }

            // Set the undo group name to avoid repeated enforcements.
            Undo.SetCurrentGroupName($"Undo $[{nameof(RequireChildComponentClassAttributeEnforcer)}]");
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
