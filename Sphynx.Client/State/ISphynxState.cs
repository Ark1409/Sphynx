namespace Sphynx.Client.State
{
    /// <summary>
    /// Represents a program state within the Sphynx Client
    /// </summary>
    public interface ISphynxState
    {
        /// <summary>
        /// Starts execution of the current state.
        /// </summary>
        /// <returns>The next state to be ran, or <c>null</c> if the program should (immediately) terminate.</returns>
        public ISphynxState? Run();
    }
}
