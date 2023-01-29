using UnityEngine;

namespace Ikonoclast.ClassAttributes.Editor
{
    using Common.Editor;

    using static Common.Editor.PanelUtils;

    internal abstract class ClassAttributeEnforcerPanel : Panel
    {
        public delegate void Request(IClassAttributeEnforcer enforcer);

        #region Fields

        public new ClassAttributeEnforcerEditorWindow editor;

        protected Rect
            mainRect,
            scrollViewRect;

        protected Vector2
            scrollViewPos = new Vector2(0, 2);

        #endregion

        #region Panel Implementations

        protected override void MakeBoxes()
        {
            MakeBox(PanelBoxRect);
        }

        #endregion
    }
}