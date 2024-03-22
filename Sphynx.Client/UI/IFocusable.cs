namespace Sphynx.Client.UI
{
    /// <summary>
    /// Represents a focusable target
    /// </summary>
    public interface IFocusable
    {
        /// <summary>
        /// Handles the key appropriately on the focus target.
        /// </summary>
        /// <param name="key">The key input to handle.</param>
        /// <returns><c>true</c> if the key was successfully handled. <c>false</c> otherwise.</returns>
        public bool HandleKey(in ConsoleKeyInfo key);
    }
}
