using System.Threading.Tasks;

namespace TomP2P.Core.P2P
{
    /// <summary>
    /// Use this interface to notify of a future has been generated from a maintenance task.
    /// </summary>
    public interface IAutomaticTask
    {
        /// <summary>
        /// Call this method when a task has been created without any user interaction.
        /// </summary>
        /// <param name="task">The task created by TomP2P.</param>
        void TaskCreated(Task task);
    }
}
