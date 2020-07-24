using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using hMailServer;
using MimeKit;

namespace MailClient.Test.TestBuilders
{
    /// <summary>
    /// Connect to existing hMailServer and manage the account
    /// </summary>
    internal class HMailServerManager : IDisposable
    {
        private readonly List<EmailAddress> _addedAccounts = new List<EmailAddress>();
        private Application _mailServer;

        internal List<Connection> Connections { get; } = new List<Connection>();
        internal string Address { get; private set; }

        internal void Connect()
        {
            string serverAddress = ConfigurationManager.AppSettings["hMailServer:Address"];
            string account = ConfigurationManager.AppSettings["hMailServer:Account"];
            string password = ConfigurationManager.AppSettings["hMailServer:Password"];
            try
            {
                _mailServer = InitComType<Application>(serverAddress);
                _mailServer.Authenticate(account, password);
                _mailServer.Connect();
                Address = serverAddress;
                PopulatePortSettings();
            }
            catch (COMException exception)
            {
                string connection = $"{account}:{password}@{serverAddress}";
                string info =
                    "Make sure you have configured server running hMailServer to allow launch permission for COM+ of hMailServer.";
                string errorMessage = $"Can't connect to hMailServer ({connection}). {info}";
                throw new InvalidOperationException(errorMessage, exception);
            }
        }

        private void PopulatePortSettings()
        {
            var validProtocols = new[] { eSessionType.eSTIMAP, eSessionType.eSTPOP3, eSessionType.eSTSMTP };
            for (int i = 0; i < _mailServer.Settings.TCPIPPorts.Count; i++)
            {
                var portSetting = _mailServer.Settings.TCPIPPorts[i];
                if (!validProtocols.Contains(portSetting.Protocol)) continue;
                string protocol = portSetting.Protocol.ToString().Replace("eST", string.Empty);
                Connections.Add(new Connection
                {
                    Port = portSetting.PortNumber,
                    Protocol = protocol,
                    UseSecureMode = portSetting.UseSSL
                });
            }
        }

        internal int GetPort(EmailAccountType protocol) =>
            Connections
                .Where(conn => conn.Protocol.Equals(protocol.ToString().ToUpper()))
                .OrderBy(conn => conn.UseSecureMode).Select(conn => conn.Port).FirstOrDefault();

        internal void AddAccount(EmailAccount emailAccount)
        {
            if (emailAccount == null) throw new ArgumentNullException(nameof(emailAccount));
            var emailAddress = EmailAddress.Parse(emailAccount.Username ?? emailAccount.Email);
            var hMailDomain = FindAndEnsureDomainAvailable(emailAddress.Domain);
            var hMailAccount = hMailDomain.Accounts.Add();
            hMailAccount.Active = true;
            hMailAccount.Address = emailAddress.ToString();
            hMailAccount.Password = emailAccount.Password;
            hMailAccount.Save();
            _addedAccounts.Add(emailAddress);
        }

        internal IEnumerable<MimeMessage> GetEmails(EmailAccount emailAccount)
        {
            var emailAddress = EmailAddress.Parse(emailAccount.Username ?? emailAccount.Email);
            var domainManager = FindAndEnsureDomainAvailable(emailAddress.Domain);
            var account = TryOrNull(() => domainManager.Accounts.ItemByAddress[emailAddress.ToString()]);
            if (account == null) yield break;

            for (int i = 0; i < account.Messages.Count; i++)
            {
                var message = account.Messages[i];
                var mimeMessage = new MimeMessage();
                string adjustedToAddress = message.To.Replace(">,", ">");
                mimeMessage.To.AddRange(InternetAddressList.Parse(adjustedToAddress));
                mimeMessage.From.Add(InternetAddress.Parse($"\"{message.From}\" <{message.FromAddress}>"));
                mimeMessage.Subject = message.Subject;
                var bodyBuilder = new BodyBuilder {HtmlBody = message.HTMLBody, TextBody = message.Body};
                mimeMessage.Body = bodyBuilder.ToMessageBody();
                yield return mimeMessage;
            }
        }

        public void AddMessage(MimeMessage mailMessage)
        {
            var hMailMessage = InitComType<Message>(Address);
            var fromAddress = (MailboxAddress)mailMessage.From.First();
            hMailMessage.From = $"\"{fromAddress.Name}\" <{fromAddress.Address}>";
            hMailMessage.FromAddress = fromAddress.Address;
            if (!string.IsNullOrWhiteSpace(mailMessage.HtmlBody))
                hMailMessage.HTMLBody = mailMessage.HtmlBody;
            if (!string.IsNullOrWhiteSpace(mailMessage.TextBody))
                hMailMessage.Body = mailMessage.TextBody;
            hMailMessage.Subject = mailMessage.Subject;
            foreach (var address in mailMessage.To.Cast<MailboxAddress>())
                hMailMessage.AddRecipient(address.Name ?? address.Address, address.Address);
            hMailMessage.Save();
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        internal void CleanUp()
        {
            foreach (var emailAccounts in _addedAccounts.GroupBy(account => account.Domain))
            {
                string domain = emailAccounts.Key;
                var domainManager = FindAndEnsureDomainAvailable(domain);
                foreach (var emailAccount in emailAccounts)
                {
                    string email = emailAccount.ToString();
                    var account = TryOrNull(() => domainManager.Accounts.ItemByAddress[email]);
                    if (account == null) continue;

                    account.DeleteMessages();
                    account.Delete();
                }

                domainManager.Delete();
            }
        }

        private Domain FindAndEnsureDomainAvailable(string domain)
        {
            var hMailDomain = TryOrNull(() => _mailServer.Domains.ItemByName[domain]);
            if (hMailDomain != null) return hMailDomain;

            hMailDomain = _mailServer.Domains.Add();
            hMailDomain.Name = domain;
            hMailDomain.Active = true;
            hMailDomain.Save();
            return hMailDomain;
        }

        private TOutput InitComType<TOutput>(string server) =>
            (TOutput)Activator.CreateInstance(Type.GetTypeFromProgID(typeof(TOutput).FullName, server));

        private TOutput TryOrNull<TOutput>(Func<TOutput> factory)
            where TOutput : class
        {
            try
            {
                return factory();
            }
            catch (COMException)
            {
                return null;
            }
        }

        private struct EmailAddress
        {
            private static readonly Regex EmailPattern = new Regex(@"(?<username>[^@]+)@(?<domain>.+)");

            internal static EmailAddress Parse(string email)
            {
                var match = EmailPattern.Match(email);
                if (!match.Success) throw new ArgumentException($"Invalid email format: {email}");
                return new EmailAddress
                {
                    Domain = match.Groups["domain"].Value,
                    Username = match.Groups["username"].Value
                };
            }

            private string Username { get; set; }
            internal string Domain { get; private set; }

            public override string ToString() => $"{Username}@{Domain}";
        }

        internal class Connection
        {
            public int Port { get; set; }
            public bool UseSecureMode { get; set; }
            public string Protocol { get; set; }
        }

        public void Dispose() => CleanUp();
    }
}
