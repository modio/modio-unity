using JetBrains.Annotations;
using ModIO.Implementation;

namespace ModIO
{
    /// <summary>
    /// Used in conjunction with ModIOUnity.Report() to send a report to the mod.io server for a
    /// specific mod.
    /// </summary>
    public class Report
    {
        /// <summary>
        /// convenience constructor for making a report. All of the parameters are mandatory to make
        /// a successful report.
        /// </summary>
        /// <param name="modId">the id of the mod being reported</param>
        /// <param name="type">the type of report</param>
        /// <param name="summary">explanation of the issue being reported</param>
        /// <param name="user">user reporting the issue</param>
        /// <param name="contactEmail">user email address</param>
        public Report(ModId modId, ReportType type, [NotNull] string summary, [NotNull] string user,
                      [NotNull] string contactEmail)
        {
            id = modId;
            this.type = type;
            resourceType = ReportResourceType.Mods;
            this.summary = summary;
            this.user = user;
            this.contactEmail = contactEmail;
        }

        public long? id;
        public string summary;
        public ReportType? type;
        public ReportResourceType? resourceType;
        public string user;
        public string contactEmail;

        public bool CanSend()
        {
            if(id == null || summary == null || type == null || resourceType == null || user == null
               || contactEmail == null)
            {
                return false;
            }

            return true;
        }
    }
}
