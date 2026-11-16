using UnityEngine;

namespace Evermeadow.Core.CharacterController.Unity
{
    /// <summary>
    /// Marks a collider as a one-way platform for the Character Controller (GDD Rule 22). The Controller filters these itself;
    /// no PlatformEffector2D is needed. Put it on the GameObject that holds the collider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OneWayPlatform : MonoBehaviour
    {
    }
}
