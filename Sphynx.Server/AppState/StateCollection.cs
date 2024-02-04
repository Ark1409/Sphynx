using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.AppState
{
    /// <summary>
    /// Represents a customizable state collection for the application.
    /// </summary>
    public class StateCollection
    {
        /// <summary>
        /// A colleciton of all existing states within the current manager.
        /// </summary>
        private readonly Dictionary<Type, ISphynxState> _states = new Dictionary<Type, ISphynxState>()
        {
            { typeof(MenuState), new MenuState() },
        };

        /// <summary>
        /// Adds a state to this state collection.
        /// </summary>
        /// <param name="state">The state to add.</param>
        public void AddState(ISphynxState state) => _states.Add(state.GetType(), state);

        /// <summary>
        /// Retrieves a state from this state collection.
        /// </summary>
        /// <param name="type">The registed type of the state to retrieve.</param>
        /// <returns>The requested state.</returns>
        public ISphynxState GetState(Type type) => _states[type];

        /// <summary>
        /// Retrieves a state from this state collection.
        /// </summary>
        /// <typeparam name="T">The registed type of the state to retrieve.</typeparam>
        /// <returns>The requested state.</returns>
        public T GetState<T>() where T : ISphynxState => (T)GetState(typeof(T));
    }
}
