using UnityEngine;

namespace Ikonoclast.ClassAttributes.Tests
{
    using RequireComponent = RequireComponentAttribute;

    [@RequireComponent(typeof(TestComponent))]
    internal class RequireComponentTest : MonoBehaviour { }
}
