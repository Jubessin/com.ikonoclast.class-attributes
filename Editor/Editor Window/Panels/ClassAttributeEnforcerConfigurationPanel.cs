using UnityEngine;

namespace Ikonoclast.ClassAttributes.Editor
{
    using Common.Editor;

    using static Common.Editor.CustomEditorHelper;

    /// ---
    /// Panel for displaying selected enforcer configurations.
    internal sealed class ClassAttributeEnforcerConfigurationPanel : ClassAttributeEnforcerPanel
    {
        #region Panel Implementations

        public override void OnPanelGUI(Vector2 size)
        {
            MakeRects(size);
            MakeBoxes();

            var selectedEnforcer = ClassAttributeEnforcerEditorWindow.SelectedEnforcer;

            if (selectedEnforcer == null)
            {
                var labelRect = new Rect
                {
                    x = mainRect.center.x - (slh * 4),
                    y = mainRect.yMin,
                    width = mainRect.width - mainRect.center.x,
                    height = mainRect.height
                };

                GUI.Label(labelRect, UnityIcons.Warn);

                labelRect.x += slh * 2;

                GUI.Label(labelRect, "No Enforcer Selected.");

                return;
            }

            scrollViewRect = new Rect
            {
                x = 0,
                y = 0,
                width = mainRect.width - 20,
                height = selectedEnforcer.ConfigurationViewHeight
            };

            scrollViewPos = GUI.BeginScrollView(mainRect, scrollViewPos, scrollViewRect, GUIStyle.none, GUIStyle.none);

            selectedEnforcer.OnConfigurationGUI(mainRect.size);

            GUI.EndScrollView();
        }

        protected override void MakeRects(Vector2 size)
        {
            float offsetX = 10f;
            float offsetY = 5f;

            PanelBoxRect = new Rect
            {
                x = offsetX,
                y = offsetY,
                width = size.x - offsetX,
                height = size.y - offsetY - 5
            };

            offsetY += 5;

            mainRect = new Rect
            {
                x = offsetX + 2,
                y = offsetY,
                width = (PanelBoxRect.width - offsetX) + 6,
                height = (PanelBoxRect.height - offsetY)
            };
        }

        #endregion
    }
}