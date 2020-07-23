# MailKitReaderWrapper
Simple MailKit wraper to fetch email with limit total fetching feature

## Requirements

### Components

- [hMailServer](https://www.hmailserver.com/)
- .NET Framework 4.7.2

### C# Library

- hMailServer .NET interop 
  Originally from [hMailServer directory]\Bin\Interop.hMailServer.dll. But it's added in this source, because not all dll works with the mail server
- NUnit
- [MailKit](https://www.mimekit.net/)

## Features

- Fetch email through POP and IMAP
- Auto delete when successfully retrieved
- Limit certain number email only when fetching