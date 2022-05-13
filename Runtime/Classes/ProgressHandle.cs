
using UnityEngine;

namespace ModIO
{
    /// <summary>
    /// A ProgressHandle can only be used to monitor the progress of an operation and cannot be
    /// used to cancel or suspend ongoing operations.
    /// The OperationType enum field specifies what type of operation this handle is for.
    /// The Progress field can be used to get the percentage (0.0 - 1.0) of the progress.
    /// The Completed and Failed fields can be used to determine if the operation is complete and
    /// whether or not it failed.
    /// </summary>
    public class ProgressHandle
    {
        /// <summary>
        /// The ModId of the mod that this operation pertains to.
        /// </summary>
        public ModId modId { get; internal set; }
        
        /// <summary>
        /// The type of operation being performed, eg. Download, Upload, Install
        /// </summary>
        public ModManagementOperationType OperationType { get; internal set; }

        /// <summary>
        /// The progress of the operation being performed, float range from 0.0f - 1.0f
        /// </summary>

        public float Progress { get; internal set; }        

        /// <summary>
        /// The average number of bytes being processed per second by the operation
        /// (Updated every 10 milliseconds)
        /// </summary>
        /// <remarks>Only applicable to Download and Upload operations</remarks>
        public long BytesPerSecond { get; internal set; }

        /// <summary>
        /// Is set to True when the operation has finished
        /// </summary>
        /// <remarks>If an operation fails then Completed will still be True, therefore it is
        /// recommended to check Failed as well</remarks>
        public bool Completed { get; internal set; }

        /// <summary>
        /// Is set to True if the operation encounters an error or is cancelled before completion
        /// </summary>
        public bool Failed { get; internal set; }
    }
}
