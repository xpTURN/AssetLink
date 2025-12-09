using System.Collections.Generic;

namespace xpTURN.Link
{
    /// <summary>
    /// This interface is used to define the UpdateLoopManager component.
    /// </summary>
    public interface IUpdateLoop
    {
        void UpdateLoop();
    }

    /// <summary>
    /// This class represents the UpdateLoopManager component.
    /// </summary>
    public class UpdateLoopManager : ComponentSingleton<UpdateLoopManager>
    {
        #region Public Methods
        /// <summary>
        /// Adds an object to the update loop.
        /// </summary>
        public void Add(IUpdateLoop updateLoop)
        {
            if (_updateLoops.Contains(updateLoop))
            {
                DebugLogger.LogError($"[UpdateLoopManager] {updateLoop} is already in the update loop.");
                return;
            }

            _updateLoops.Add(updateLoop);
        }
        /// <summary>
        /// Removes an object from the update loop.
        /// </summary>
        public void Remove(IUpdateLoop updateLoop)
        {
            if (!_updateLoops.Contains(updateLoop))
            {
                DebugLogger.LogError($"[UpdateLoopManager] {updateLoop} is not in the update loop.");
                return;
            }

            _updateLoops.Remove(updateLoop);
        }
        #endregion

        #region Private Fields
        List<IUpdateLoop> _updateLoops = new ();
        #endregion

        #region Private Methods
        private void Update()
        {
            foreach (var updateLoop in _updateLoops)
            {
                updateLoop.UpdateLoop();
            }
        }
        #endregion
    }
}
