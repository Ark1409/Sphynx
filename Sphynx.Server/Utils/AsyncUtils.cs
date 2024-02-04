namespace Sphynx.Server.Utils
{
    internal static class AsyncUtils
    {
        /// <summary>
        /// Executes a <see cref="Task"/> asynchronously while propagating exceptions.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="continueOnCapturedContext">Whether to continue on the same sync. context. Not applicable to .NET
        /// Core but is still good practice to support.</param>
        internal static async void SafeExecute(this Task task, bool continueOnCapturedContext = true)
        {
            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
            }
            catch
            {
                // TODO: Handle exception
                throw;
            }
        }
        
        /// <summary>
        /// Executes a <see cref="ValueTask"/> asynchronously while propagating exceptions.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="continueOnCapturedContext">Whether to continue on the same sync. context. Not applicable to .NET
        /// Core but is still good practice to support.</param>
        internal static async void SafeExecute(this ValueTask task, bool continueOnCapturedContext = true)
        {
            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
            }
            catch
            {
                // TODO: Handle exception
                throw;
            }
        }
    }
}
