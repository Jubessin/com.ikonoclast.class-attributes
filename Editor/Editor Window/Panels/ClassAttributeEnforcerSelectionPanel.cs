using UnityEngine;

namespace Ikonoclast.ClassAttributes.Editor
{
    using Common;

    using static Common.Editor.CustomEditorHelper;

    internal sealed class ClassAttributeEnforcerSelectionPanel : ClassAttributeEnforcerPanel
    {
        #region Events

        public static event Request RequestSelectEnforcer;

        #endregion

        #region Panel Implementations

        public override void OnPanelGUI(Vector2 size)
        {
            MakeRects(size);
            MakeBoxes();

            scrollViewPos = GUI.BeginScrollView(mainRect, scrollViewPos, scrollViewRect, GUIStyle.none, GUIStyle.none);

            var selectionRect = new Rect
            {
                x = 2,
                y = 2,
                width = mainRect.width - 4,
                height = slh * 1.5f
            };

            foreach (var item in ClassAttributeEnforcerUtilities.Registered)
            {
                if (ClassAttributeEnforcerEditorWindow.SelectedEnforcer == item)
                {
                    GUI.color = Colors.Yellow;
                }

                if (GUI.Button(selectionRect, item.GetType().Name))
                {
                    RequestSelectEnforcer?.Invoke(item);
                }

                ResetGUIColor();

                selectionRect.y += selectionRect.height + 1.5f;
            }

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

            var amountRegistered = Mathf.Ceil(ClassAttributeEnforcerUtilities.Registered.Count);

            scrollViewRect = new Rect
            {
                x = 0,
                y = 0,
                width = mainRect.width - offsetX - 10,
                height = (slh * 1.5f * amountRegistered) + (amountRegistered * 1.5f) + 2
            };
        }

        #endregion
    }
}