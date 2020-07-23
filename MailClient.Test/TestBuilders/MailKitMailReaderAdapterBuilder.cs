using System;
using MimeKit;

namespace MailClient.Test.TestBuilders
{
    internal class MailKitMailReaderAdapterBuilder
    {
        private readonly MailKitMailReaderAdapter _inner = new MailKitMailReaderAdapter();

        public static MailKitMailReaderAdapterBuilder New => new MailKitMailReaderAdapterBuilder();

        public static implicit operator MailKitMailReaderAdapter(MailKitMailReaderAdapterBuilder builder) =>
            builder._inner;

        public MailKitMailReaderAdapterBuilder WithOnReceivedCallback(Action<MimeMessage> callback)
        {
            _inner.RemoteMailLoaded += (sender, args) => callback(args.MailMessage);
            return this;
        }
    }
}
