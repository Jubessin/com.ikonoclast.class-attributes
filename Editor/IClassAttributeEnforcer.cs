namespace Ikonoclast.ClassAttributes.Editor
{
    using Common.Editor;
    using UnityEngine;

    public interface IClassAttributeEnforcer : IEditorSaveObject
    {
        float ConfigurationViewHeight
        {
            get;
        }

        void OnConfigurationGUI(Vector2 size);
    }
}
