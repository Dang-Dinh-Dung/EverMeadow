namespace Evermeadow.Foundation
{
    /// <summary>
    /// Logging seam for pure-C# cores. Injected so tests can count warnings and errors by key
    /// (Input GDD test hook H-6); the Unity adapter forwards to <c>UnityEngine.Debug</c>.
    /// </summary>
    public interface ILogSink
    {
        /// <summary>Reports a recoverable problem. <paramref name="key"/> identifies the kind of warning.</summary>
        void Warn(string key, string message);

        /// <summary>Reports a defect the system recovered from. <paramref name="key"/> identifies the kind of error.</summary>
        void Error(string key, string message);
    }

    /// <summary>Log sink that discards everything. Default when a caller does not inject one.</summary>
    public sealed class NullLogSink : ILogSink
    {
        /// <inheritdoc />
        public void Warn(string key, string message) { }

        /// <inheritdoc />
        public void Error(string key, string message) { }
    }
}
