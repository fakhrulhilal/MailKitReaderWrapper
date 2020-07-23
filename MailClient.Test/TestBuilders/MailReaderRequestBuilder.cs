namespace MailClient.Test.TestBuilders
{
    internal class MailReaderRequestBuilder
    {
        private readonly MailReaderRequest _inner = new MailReaderRequest
        { Account = new EmailAccount() };

        public static MailReaderRequestBuilder New => new MailReaderRequestBuilder();

        public static implicit operator MailReaderRequest(MailReaderRequestBuilder builder) => builder._inner;

        internal MailReaderRequestBuilder WithAccount(EmailAccount emailAccount)
        {
            _inner.Account = emailAccount;
            return this;
        }

        internal MailReaderRequestBuilder WithLimitation(int total)
        {
            _inner.LimitTotalFetching = total;
            return this;
        }

        internal MailReaderRequestBuilder WithoutLimitation()
        {
            _inner.LimitTotalFetching = null;
            return this;
        }

        internal MailReaderRequestBuilder WithAutoDeleteEnabled()
        {
            _inner.DoAutoDelete = true;
            return this;
        }

        internal MailReaderRequestBuilder WithAutoDeleteDisabled()
        {
            _inner.DoAutoDelete = false;
            return this;
        }
    }
}
