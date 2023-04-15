using System;
using UnityEditor;
using UnityEngine;

namespace Ikonoclast.ClassAttributes.Editor
{
    using Common.Editor;

    using static Common.Editor.CustomEditorHelper;

    /// <summary>
    /// Editor window for configuring class attribute enforcers.
    /// </summary>
    internal sealed class ClassAttributeEnforcerEditorWindow : EditorWindow, ICustomEditorWindow
    {
        #region Fields

        private int?
            selectionWindowID,
            configurationWindowID;

        private Panel
            panel1,
            panel2;

        private ClassAttributeEnforcerSelectionPanel selectionPanel;
        private ClassAttributeEnforcerConfigurationPanel configurationPanel;

        private Rect
            panel1WindowRect,
            panel2WindowRect;

        private static Vector2
            panel1Size,
            panel2Size;

        private static Rect lastPosition = Rect.zero;

        public static Action RequestAttributeEnforcement;

        #endregion

        #region Properties

        private ICustomEditorWindow CustomEditorWindow => this;

        public static IClassAttributeEnforcer SelectedEnforcer
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        private void Subscribe()
        {
            ClassAttributeEnforcerSelectionPanel.RequestSelectEnforcer += OnRequestSelectEnforcer;
        }

        private void Unsubscribe()
        {
            ClassAttributeEnforcerSelectionPanel.RequestSelectEnforcer -= OnRequestSelectEnforcer;
        }

        [MenuItem("Ikonoclast/Class Attribute Enforcer Configurations")]
        private static void OpenEditor()
        {
            if (HasOpenInstances<ClassAttributeEnforcerEditorWindow>())
                return;

            var window = GetWindow<ClassAttributeEnforcerEditorWindow>();

            lastPosition = window.position;

            // Initialize window parameters.
            window.maximized = false;
            window.minSize = new Vector2(450, 250);
            window.titleContent = new GUIContent("Class Attribute Enforcer Configurations");

            panel1Size = panel2Size = new Vector2
            {
                x = lastPosition.size.x - 10,
                y = lastPosition.size.y * 0.5f
            };

            window.Show();
        }

        #endregion

        #region Event Listeners

        private void OnRequestSelectEnforcer(IClassAttributeEnforcer enforcer)
        {
            SelectedEnforcer = SelectedEnforcer == enforcer
                ? null
                : enforcer;
        }

        #endregion

        #region ICustomEditorWindow Implementations

        bool ICustomEditorWindow.IsMouseInFocusable
        {
            get;
        }

        void ICustomEditorWindow.HandleWindowReposition()
        {
            if (position == lastPosition)
                return;

            var size = position.size;

            // Resize panels.
            panel1Size = panel2Size = new Vector2
            {
                x = size.x - 10,
                y = size.y * 0.5f
            };
        }

        void ICustomEditorWindow.AddFocusable(Rect rect) { }

        void ICustomEditorWindow.RemoveFocusable(Rect rect) { }

        #endregion

        private void OnGUI()
        {
            HandleEvent(out bool close);

            if (close)
                return;

            HandleWindow();

            void HandleEvent(out bool close)
            {
                close = false;

                var evt = Event.current;

                if (evt == null)
                    return;

                if (evt.type != EventType.KeyDown)
                    return;

                if (evt.keyCode == KeyCode.Escape)
                {
                    Close();

                    close = true;
                }
            }
            void HandleWindow()
            {
                panel1WindowRect = new Rect(0, 0, panel1Size.x, panel1Size.y);
                panel2WindowRect = new Rect(0, panel1Size.y, panel2Size.x, panel2Size.y);

                BeginWindows();

                // Invoke OnPanelGUI on both panels, supplying the rect that will serve as their bounds.
                GUI.Window(
                    selectionWindowID ??= GenerateUniqueSessionID(),
                    panel1WindowRect,
                    (_) => panel1.OnPanelGUI(panel1Size),
                    "",
                    EditorStyles.inspectorDefaultMargins);

                GUI.Window(
                    configurationWindowID ??= GenerateUniqueSessionID(),
                    panel2WindowRect,
                    (_) => panel2.OnPanelGUI(panel2Size),
                    "",
                    EditorStyles.inspectorDefaultMargins);

                EndWindows();

                CustomEditorWindow.HandleWindowReposition();
            }
        }

        private void OnEnable()
        {
            selectionPanel = new ClassAttributeEnforcerSelectionPanel();
            configurationPanel = new ClassAttributeEnforcerConfigurationPanel();

            panel1 = selectionPanel;
            panel2 = configurationPanel;

            Subscribe();

            EditorGUIUtility.SetIconSize(Vector2.zero);
            SetDefaultGUIColor(GUI.color);
            SetDefaultGUIContentColor(GUI.contentColor);
        }

        private void OnDisable()
        {
            Unsubscribe();

            // Cache whether any configurations have changed.
            var shouldInvokeEnforcement = ClassAttributeEnforcerUtilities.ShouldReloadConfiguration;

            ClassAttributeEnforcerUtilities.SaveConfigurationFile();

            if (shouldInvokeEnforcement)
            {
                RequestAttributeEnforcement();
            }
        }
    }
}