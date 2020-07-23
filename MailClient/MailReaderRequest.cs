namespace MailClient
{
    public class MailReaderRequest
    {
        /// <summary>
        /// Email account to connect to incoming server
        /// </summary>
        public EmailAccount Account { get; set; }

        /// <summary>
        /// Determine whether to delete the email automatically after fetched
        /// </summary>
        public bool DoAutoDelete { get; set; }

        /// <summary>
        /// Limit the number of fetched email, first received email will be retrieved first
        /// </summary>
        public int? LimitTotalFetching { get; set; }
    }
}
