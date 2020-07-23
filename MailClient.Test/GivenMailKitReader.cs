using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailClient.Test.TestBuilders;
using MimeKit;
using NUnit.Framework;
using Request = MailClient.Test.TestBuilders.MailReaderRequestBuilder;
using SUT = MailClient.MailKitMailReaderAdapter;
using TestBuilder = MailClient.Test.TestBuilders.MailKitMailReaderAdapterBuilder;

namespace MailClient.Test
{
    /// <summary>
    /// Collection of test scenario reading email from true mail server.
    /// It requires active hMailServer with DCOM permission configured for launch remotely.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    class GivenMailKitReader
    {
        [Test]
        [TestCase(EmailAccountType.Imap)]
        [TestCase(EmailAccountType.Pop3)]
        public async Task WhenLoadingFromRemoteServerThenItWillEmailDetail(EmailAccountType protocol)
        {
            using (var mailServer = new HMailServerManager())
            {
                mailServer.Connect();
                var receivedMessages = new List<MimeMessage>();
                SUT sut = TestBuilder.New.WithOnReceivedCallback(receivedMessages.Add);
                MailReaderRequest request = Request.New.WithAccount(EmailAccountBuilder.New
                    .WithProtocol(protocol)
                    .WithMailServer(mailServer));
                mailServer.AddAccount(request.Account);
                var email = MailMessageBuilder.New.WithToAddress(request.Account).Build();
                mailServer.AddMessage(email);

                await sut.LoadFromServerAsync(request, CancellationToken.None);

                var actualMessage = receivedMessages.FirstOrDefault();
                Assert.That(actualMessage, Is.Not.Null);
                Assert.That(actualMessage.Subject, Is.EqualTo(email.Subject));
                Assert.That(actualMessage.TextBody, Is.EqualTo(email.TextBody).Using(StringCompare.IgnoreLineBreak));
                Assert.That(actualMessage.To.Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(actualMessage.To.First(), Is.EqualTo(email.To.FirstOrDefault()));
                Assert.That(actualMessage.From.FirstOrDefault(), Is.Not.Null.And.EqualTo(email.From.FirstOrDefault()));
            }
        }

        [Test]
        [TestCase(EmailAccountType.Imap)]
        [TestCase(EmailAccountType.Pop3)]
        public async Task WhenAutoDeleteEnabledThenItWillDeleteEmailAfterFetched(EmailAccountType protocol)
        {
            using (var mailServer = new HMailServerManager())
            {
                mailServer.Connect();
                SUT sut = TestBuilder.New;
                MailReaderRequest request = Request.New
                    .WithAccount(EmailAccountBuilder.New
                        .WithProtocol(protocol)
                        .WithMailServer(mailServer))
                    .WithAutoDeleteEnabled();
                mailServer.AddAccount(request.Account);
                var email = MailMessageBuilder.New
                    .WithToAddress(request.Account)
                    .WithSubject(Guid.NewGuid().ToString())
                    .Build();
                mailServer.AddMessage(email);

                await sut.LoadFromServerAsync(request, CancellationToken.None);

                var emails = mailServer.GetEmails(request.Account).ToArray();
                Assert.That(emails.Select(e => e.Subject), Does.Not.Contain(email.Subject));
            }
        }

        [Test]
        [TestCase(EmailAccountType.Imap)]
        [TestCase(EmailAccountType.Pop3)]
        public async Task WhenAutoDeleteDisabledThenItWillKeepEmailAfterFetched(EmailAccountType protocol)
        {
            using (var mailServer = new HMailServerManager())
            {
                mailServer.Connect();
                SUT sut = TestBuilder.New;
                MailReaderRequest request = Request.New
                    .WithAccount(EmailAccountBuilder.New
                        .WithProtocol(protocol)
                        .WithMailServer(mailServer))
                    .WithAutoDeleteDisabled();
                mailServer.AddAccount(request.Account);
                var email = MailMessageBuilder.New
                    .WithToAddress(request.Account)
                    .WithSubject(Guid.NewGuid().ToString())
                    .Build();
                mailServer.AddMessage(email);

                await sut.LoadFromServerAsync(request, CancellationToken.None);

                var emails = mailServer.GetEmails(request.Account).ToArray();
                Assert.That(emails.Select(e => e.Subject), Does.Contain(email.Subject));
            }
        }

        [Test]
        [TestCase(EmailAccountType.Imap)]
        [TestCase(EmailAccountType.Pop3)]
        public async Task WhenNoLimitDefinedThenItWillFetchAllEmails(EmailAccountType protocol)
        {
            using (var mailServer = new HMailServerManager())
            {
                mailServer.Connect();
                var receivedMessages = new List<MimeMessage>();
                SUT sut = TestBuilder.New.WithOnReceivedCallback(receivedMessages.Add);
                const int totalEmail = 3;
                MailReaderRequest request = Request.New
                    .WithAccount(EmailAccountBuilder.New
                        .WithProtocol(protocol)
                        .WithMailServer(mailServer))
                    .WithoutLimitation();
                mailServer.AddAccount(request.Account);
                var emailSubjects = new List<string>();
                for (int i = 0; i < totalEmail; i++)
                {
                    string subject = Guid.NewGuid().ToString();
                    var email = MailMessageBuilder.New
                        .WithToAddress(request.Account)
                        .WithSubject(subject)
                        .Build();
                    mailServer.AddMessage(email);
                    emailSubjects.Add(subject);
                }

                await sut.LoadFromServerAsync(request, CancellationToken.None);

                Assert.That(receivedMessages.Count, Is.EqualTo(totalEmail));
                Assert.That(receivedMessages.Select(e => e.Subject), Is.EquivalentTo(emailSubjects));
            }
        }

        [Test]
        [TestCase(EmailAccountType.Imap)]
        [TestCase(EmailAccountType.Pop3)]
        public async Task WhenLimitDefinedAndTotalEmailIsGreaterThanLimitationThenItWillFetchByLimitationOnly(EmailAccountType protocol)
        {
            using (var mailServer = new HMailServerManager())
            {
                mailServer.Connect();
                var receivedMessages = new List<MimeMessage>();
                SUT sut = TestBuilder.New.WithOnReceivedCallback(receivedMessages.Add);
                const int totalEmail = 3;
                MailReaderRequest request = Request.New
                    .WithAccount(EmailAccountBuilder.New
                        .WithProtocol(protocol)
                        .WithMailServer(mailServer))
                    .WithLimitation(totalEmail - 1);
                mailServer.AddAccount(request.Account);
                for (int i = 0; i < totalEmail; i++)
                {
                    string subject = Guid.NewGuid().ToString();
                    var email = MailMessageBuilder.New
                        .WithToAddress(request.Account)
                        .WithSubject(subject)
                        .Build();
                    mailServer.AddMessage(email);
                }

                await sut.LoadFromServerAsync(request, CancellationToken.None);

                Assert.That(receivedMessages.Count, Is.EqualTo(request.LimitTotalFetching));
            }
        }

        [Test]
        [TestCase(EmailAccountType.Imap)]
        [TestCase(EmailAccountType.Pop3)]
        public async Task WhenLimitDefinedAndTotalEmailIsLessThanLimitationThenItWillFetchAllEmails(EmailAccountType protocol)
        {
            using (var mailServer = new HMailServerManager())
            {
                mailServer.Connect();
                var receivedMessages = new List<MimeMessage>();
                SUT sut = TestBuilder.New.WithOnReceivedCallback(receivedMessages.Add);
                const int totalEmail = 3;
                MailReaderRequest request = Request.New
                    .WithAccount(EmailAccountBuilder.New
                        .WithProtocol(protocol)
                        .WithMailServer(mailServer))
                    .WithLimitation(totalEmail + 1);
                mailServer.AddAccount(request.Account);
                var emailSubjects = new List<string>();
                for (int i = 0; i < totalEmail; i++)
                {
                    string subject = Guid.NewGuid().ToString();
                    var email = MailMessageBuilder.New
                        .WithToAddress(request.Account)
                        .WithSubject(subject)
                        .Build();
                    mailServer.AddMessage(email);
                    emailSubjects.Add(subject);
                }

                await sut.LoadFromServerAsync(request, CancellationToken.None);

                Assert.That(receivedMessages.Count, Is.EqualTo(totalEmail));
                Assert.That(receivedMessages.Select(e => e.Subject), Is.EquivalentTo(emailSubjects));
            }
        }

        [Test]
        [TestCase(EmailAccountType.Imap)]
        [TestCase(EmailAccountType.Pop3)]
        public async Task WhenLimitDefinedThenItWillFetchElderEmailFirst(EmailAccountType protocol)
        {
            using (var mailServer = new HMailServerManager())
            {
                mailServer.Connect();
                var receivedMessages = new List<MimeMessage>();
                SUT sut = TestBuilder.New.WithOnReceivedCallback(receivedMessages.Add);
                const int totalEmail = 3;
                MailReaderRequest request = Request.New
                    .WithAccount(EmailAccountBuilder.New
                        .WithProtocol(protocol)
                        .WithMailServer(mailServer))
                    .WithLimitation(totalEmail - 1);
                mailServer.AddAccount(request.Account);
                var emailSubjects = new List<string>();
                for (int i = 1; i <= totalEmail; i++)
                {
                    string subject = $"Subject for-{i}";
                    var email = MailMessageBuilder.New
                        .WithToAddress(request.Account)
                        .WithSubject(subject)
                        .Build();
                    mailServer.AddMessage(email);
                    emailSubjects.Add(subject);
                }

                await sut.LoadFromServerAsync(request, CancellationToken.None);

                var expectedSubjects = emailSubjects.Take(request.LimitTotalFetching ?? default).ToArray();
                Assert.That(receivedMessages.Select(e => e.Subject), Is.EquivalentTo(expectedSubjects).And.Ordered);
            }
        }
    }
}
