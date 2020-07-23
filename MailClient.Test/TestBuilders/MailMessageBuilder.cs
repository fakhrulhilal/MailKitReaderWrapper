using System;
using MimeKit;

namespace MailClient.Test.TestBuilders
{
    internal class MailMessageBuilder
    {
        private readonly MimeMessage _inner = new MimeMessage();

        internal static MailMessageBuilder New => new MailMessageBuilder();

        internal MimeMessage Build() => _inner;

        public MailMessageBuilder()
        {
            _inner.MessageId = Guid.NewGuid().ToString();
            _inner.To.Add(InternetAddress.Parse("to@UnitTest.local"));
            _inner.From.Add(InternetAddress.Parse("Sender <from@UnitTest.local>"));
            _inner.Subject = $"Random subject: {Guid.NewGuid()}";
            string body = $"Random body: {Guid.NewGuid()}";
            _inner.Body = new BodyBuilder
            {
                TextBody = body,
                HtmlBody = body
            }.ToMessageBody();
        }

        internal MailMessageBuilder WithSubject(string subject)
        {
            _inner.Subject = subject;
            return this;
        }

        internal MailMessageBuilder WithToAddress(string email)
        {
            _inner.To.AddRange(InternetAddressList.Parse(email));
            return this;
        }

        internal MailMessageBuilder WithToAddress(EmailAccount emailAccount) =>
            WithToAddress($"\"{emailAccount.Alias}\" <{emailAccount.Email}>");
    }
}
