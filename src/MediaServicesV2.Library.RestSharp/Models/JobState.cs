namespace MediaServicesV2.Library.RestSharp.Models
{
    /// <summary>
    /// The media services v2 job states.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// Queued state
        /// </summary>
        Queued = 0,

        /// <summary>
        /// Scheduled state
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// Processing state
        /// </summary>
        Processing = 2,

        /// <summary>
        /// Finished state
        /// </summary>
        Finished = 3,

        /// <summary>
        /// Error state
        /// </summary>
        Error = 4,

        /// <summary>
        /// Canceled state
        /// </summary>
        Canceled = 5,

        /// <summary>
        /// Canceling state
        /// </summary>
        Canceling = 6
    }
}
