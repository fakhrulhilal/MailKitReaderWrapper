using System;

namespace MailClient.Test.TestBuilders
{
    internal class EmailAccountBuilder
    {
        private readonly EmailAccount _inner;

        public EmailAccountBuilder()
        {
            string id = Guid.NewGuid().ToString();
            _inner = new EmailAccount
            {
                Username = $"{id}@UnitTest.local",
                Password = "P@ssw0rd",
                Mailbox = "INBOX",
                Type = EmailAccountType.Imap
            };
            _inner.Email = _inner.Username;
            _inner.Alias = $"Dummy Account {id}";
        }

        public static EmailAccountBuilder New => new EmailAccountBuilder();

        public static implicit operator EmailAccount(EmailAccountBuilder builder) => builder._inner;

        public EmailAccountBuilder WithMailServer(HMailServerManager mailServer)
        {
            _inner.IncomingAddress = mailServer.Address;
            _inner.Port = mailServer.GetPort(_inner.Type);
            return this;
        }

        public EmailAccountBuilder WithProtocol(EmailAccountType protocol)
        {
            _inner.Type = protocol;
            return this;
        }
    }
}
