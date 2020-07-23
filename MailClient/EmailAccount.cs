namespace MailClient
{
    public class EmailAccount
    {
        public string Email { get; set; }
        public EmailAccountType Type { get; set; }

        /// <summary>
        /// Username to connect to email server
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password to connect to email server
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Incoming server address (POP3/IMAP)
        /// </summary>
        public string IncomingAddress { get; set; }

        /// <summary>
        /// Incoming server port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Determine whether the connection to the server need secure connection or not (TLS/SSL)
        /// </summary>
        public bool EnableSecureProtocol { get; set; }

        /// <summary>
        /// Mailbox name for IMAP
        /// </summary>
        public string Mailbox { get; set; }

        public string Alias { get; set; }
    }

    public enum EmailAccountType
    {
        DropFolder = 0,
        Pop3 = 1,
        Imap = 2
    }
}
