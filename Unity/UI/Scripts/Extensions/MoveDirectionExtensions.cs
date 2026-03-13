using UnityEngine.EventSystems;

namespace Modio.Unity.UI.Extensions
{
    public static class MoveDirectionExtensions
    {
        /// <summary>
        /// Convenience method that converts a move direction to an <see cref="AxisEventData"/> for use in forwarding
        /// received movement directions
        /// </summary>
        public static AxisEventData ToAxisEventData(this MoveDirection? direction)
            => new AxisEventData(EventSystem.current) { moveDir = direction ?? MoveDirection.None, };
        
        /// <summary>
        /// Convenience method that converts a move direction to an <see cref="AxisEventData"/> for use in forwarding
        /// received movement directions
        /// </summary>
        public static AxisEventData ToAxisEventData(this MoveDirection direction)
            => new AxisEventData(EventSystem.current) { moveDir = direction, };
    }
}
