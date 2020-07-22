// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    var jobInfo = JobInfo.FromJson(jsonString);

using System;
using Newtonsoft.Json;

namespace MediaServicesV2.Library.RestSharp.Models
{
    /// <summary>
    /// Class to manage job info.
    /// </summary>
    public partial class JobInfo
    {
        /// <summary>
        /// Gets or sets the job metadata.
        /// </summary>
        [JsonProperty("__metadata")]
        public JobInfoMetadata Metadata { get; set; }

        /// <summary>
        /// Gets or sets the tasks.
        /// </summary>
        [JsonProperty("Tasks")]
        public InputMediaAssets Tasks { get; set; }

        /// <summary>
        /// Gets or sets the output media assets.
        /// </summary>
        [JsonProperty("OutputMediaAssets")]
        public InputMediaAssets OutputMediaAssets { get; set; }

        /// <summary>
        /// Gets or sets the the input media assets.
        /// </summary>
        [JsonProperty("InputMediaAssets")]
        public InputMediaAssets InputMediaAssets { get; set; }

        /// <summary>
        /// Gets or sets the job Id.
        /// </summary>
        [JsonProperty("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the job name.
        /// </summary>
        [JsonProperty("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the job creation time.
        /// </summary>
        [JsonProperty("Created")]
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Gets or sets the job last modified time.
        /// </summary>
        [JsonProperty("LastModified")]
        public DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// Gets or sets the job end time.
        /// </summary>
        [JsonProperty("EndTime")]
        public object EndTime { get; set; }

        /// <summary>
        /// Gets or sets the job priority.
        /// </summary>
        [JsonProperty("Priority")]
        public long Priority { get; set; }

        /// <summary>
        /// Gets or sets the job running duration.
        /// </summary>
        [JsonProperty("RunningDuration")]
        public long RunningDuration { get; set; }

        /// <summary>
        /// Gets or sets the job start time.
        /// </summary>
        [JsonProperty("StartTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Gets or sets the job state.
        /// </summary>
        [JsonProperty("State")]
        public JobState State { get; set; }

        /// <summary>
        /// Gets or sets the job template Id.
        /// </summary>
        [JsonProperty("TemplateId")]
        public object TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the job notification subscriptions.
        /// </summary>
        [JsonProperty("JobNotificationSubscriptions")]
        public JobNotificationSubscriptions JobNotificationSubscriptions { get; set; }
    }

    /// <summary>
    /// Input media assets object.
    /// </summary>
    public partial class InputMediaAssets
    {
        /// <summary>
        /// Gets or sets the deffered.
        /// </summary>
        [JsonProperty("__deferred")]
        public Deferred Deferred { get; set; }
    }

    /// <summary>
    /// Deferred object.
    /// </summary>
    public partial class Deferred
    {
        /// <summary>
        /// Gets or sets the uri.
        /// </summary>
        [JsonProperty("uri")]
        public Uri Uri { get; set; }
    }

    /// <summary>
    /// Job notification subscriptions object.
    /// </summary>
    public partial class JobNotificationSubscriptions
    {
        /// <summary>
        ///  Gets or sets the metadata.
        /// </summary>
        [JsonProperty("__metadata")]
        public JobNotificationSubscriptionsMetadata Metadata { get; set; }

        /// <summary>
        ///  Gets or sets the results.
        /// </summary>
        [JsonProperty("results")]
#pragma warning disable CA1819 // Properties should not return arrays
        public object[] Results { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }

    /// <summary>
    /// Job notification subscriptions metadata.
    /// </summary>
    public partial class JobNotificationSubscriptionsMetadata
    {
        /// <summary>
        /// Gets or sets  the type.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Job info metadata object.
    /// </summary>
    public partial class JobInfoMetadata
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty("id")]
        public Uri Id { get; set; }

        /// <summary>
        /// Gets or sets the uri.
        /// </summary>
        [JsonProperty("uri")]
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
