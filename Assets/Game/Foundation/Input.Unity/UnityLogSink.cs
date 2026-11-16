using UnityEngine;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>Forwards core log messages to the Unity console, prefixed with their key.</summary>
    public sealed class UnityLogSink : ILogSink
    {
        /// <inheritdoc />
        public void Warn(string key, string message) => Debug.LogWarning($"[{key}] {message}");

        /// <inheritdoc />
        public void Error(string key, string message) => Debug.LogError($"[{key}] {message}");
    }
}
