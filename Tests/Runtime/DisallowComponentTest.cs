using UnityEngine;

namespace Ikonoclast.ClassAttributes.Tests
{
    [DisallowComponent(typeof(TestComponent))]
    internal class DisallowComponentTest : MonoBehaviour { }
}
