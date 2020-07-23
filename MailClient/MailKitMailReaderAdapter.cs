using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace MailClient
{
    /// <summary>
    /// Adapter for mail reader using MailKit library
    /// </summary>
    public class MailKitMailReaderAdapter
    {
        public event EventHandler<LoadMailErrorEventArgs> LoadMailError;
        public event EventHandler<DeleteMailErrorEventArgs> DeleteMailError;
        public event EventHandler<RemoteMailLoadedEventArgs> RemoteMailLoaded;

        public async Task LoadFromServerAsync(MailReaderRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var account = request.Account ?? throw new ArgumentNullException(nameof(request.Account));
            if (account.Type != EmailAccountType.Imap && account.Type != EmailAccountType.Pop3)
                throw new InvalidOperationException($"Invalid email type: {account.Type}");
            using (var client = account.Type == EmailAccountType.Imap
                ? (IMailService)new ImapClient()
                : new Pop3Client())
            {
                var container = await InitializeAndGetContainerAsync(client, account, cancellationToken);
                var ids = await GetMessageIdsAsync(container, request.LimitTotalFetching, cancellationToken);
                foreach (var id in ids)
                {
                    try
                    {
                        var mimeMessage = await GetMessageAsync(container, id, cancellationToken);
                        RemoteMailLoaded?.Invoke(this, new RemoteMailLoadedEventArgs
                        {
                            MailMessage = mimeMessage,
                            Account = account
                        });
                    }
                    catch (Exception exception) when (exception.NeedNotBeAvoided())
                    {
                        LoadMailError?.Invoke(this, new LoadMailErrorEventArgs
                        {
                            Account = account,
                            Exception = exception,
                            Id = id.ToString()
                        });
                        continue;
                    }

                    if (!request.DoAutoDelete) continue;
                    try
                    {
                        await DeleteMessageAsync(container, id, cancellationToken);
                    }
                    catch (Exception exception) when (exception.NeedNotBeAvoided())
                    {
                        DeleteMailError?.Invoke(this, new DeleteMailErrorEventArgs
                        {
                            Account = account,
                            Id = id.ToString(),
                            Exception = exception
                        });
                    }
                }

                await client.DisconnectAsync(true, cancellationToken);
            }
        }

        private async Task DeleteMessageAsync(object container, object id, CancellationToken cancellationToken)
        {
            switch (container)
            {
                case IMailFolder mailbox when id is UniqueId uid:
                    await mailbox.AddFlagsAsync(uid, MessageFlags.Deleted, true, cancellationToken);
                    await mailbox.ExpungeAsync(cancellationToken);
                    return;
                case Pop3Client client when id is int index:
                    await client.DeleteMessageAsync(index, cancellationToken);
                    return;
                default:
                    throw new InvalidOperationException("Invalid mail client container or ID");
            }
        }

        private async Task<object[]> GetMessageIdsAsync(object container,
            int? limit, CancellationToken cancellationToken)
        {
            switch (container)
            {
                case IMailFolder mailbox:
                    var ids = await mailbox.SearchAsync(SearchQuery.All, cancellationToken);
                    int totalFetchImap = limit.HasValue && ids.Count > limit ? limit.Value : ids.Count;
                    return ids.Take(totalFetchImap).Cast<object>().ToArray();
                case Pop3Client pop3Client:
                    var totalNewMessage = await pop3Client.GetMessageCountAsync(cancellationToken);
                    int totalFetchPop3 = limit.HasValue && totalNewMessage > limit
                        ? limit.Value
                        : totalNewMessage;
                    return Enumerable.Range(0, totalFetchPop3).Cast<object>().ToArray();
                default:
                    throw new InvalidOperationException("Invalid container");
            }
        }

        private async Task<MimeMessage> GetMessageAsync(object container, object id,
            CancellationToken cancellationToken)
        {
            if (container is IMailFolder folder && id is UniqueId uid)
                return await folder.GetMessageAsync(uid, cancellationToken);
            if (container is Pop3Client client && id is int index)
                return await client.GetMessageAsync(index, cancellationToken);

            throw new InvalidOperationException("Invalid mail client container");
        }

        private async Task<object> InitializeAndGetContainerAsync(IMailService client, EmailAccount account,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(account.Username))
                throw new ApplicationException(
                    $"Receiving account username can't be empty: {account.Email}");
            string hostname = !IPAddress.TryParse(account.IncomingAddress, out var address) ||
                              address.AddressFamily == AddressFamily.InterNetwork
                ? account.IncomingAddress
                : $"[{account.IncomingAddress}]";
            if (account.EnableSecureProtocol)
            {
                client.SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
                client.ServerCertificateValidationCallback = (sender, certificate, chain, errorPolicies) =>
                    errorPolicies != SslPolicyErrors.RemoteCertificateNotAvailable;
                await client.ConnectAsync(hostname, account.Port, true, cancellationToken);
            }
            else
            {
                client.SslProtocols = SslProtocols.None;
                await client.ConnectAsync(hostname, account.Port, SecureSocketOptions.None,
                    cancellationToken);
            }

            await client.AuthenticateAsync(account.Username, account.Password, cancellationToken);

            if (!(client is ImapClient imapClient)) return client;

            var mailbox = await imapClient.GetFolderAsync(account.Mailbox ?? "INBOX", cancellationToken);
            await mailbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            return mailbox;
        }
    }

    public class LoadMailErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Unique ID of the email.
        /// In POP3, it's sequence number.
        /// In IMAP, it's unique email ID from the mailbox.
        /// In local file, it's full path to EML file.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Account to connect to server (if it's remote mail server)
        /// </summary>
        public EmailAccount Account { get; set; }

        /// <summary>
        /// Error exception
        /// </summary>
        public Exception Exception { get; set; }
    }

    public class DeleteMailErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Unique ID of the email.
        /// In POP3, it's sequence number.
        /// In IMAP, it's unique email ID from the mailbox.
        /// In local file, it's full path to EML file.
        /// </summary>
        public string Id { get; set; }

        public Exception Exception { get; set; }
        public EmailAccount Account { get; set; }
    }

    public class RemoteMailLoadedEventArgs : EventArgs
    {
        public MimeMessage MailMessage { get; set; }
        public EmailAccount Account { get; set; }
    }
}
