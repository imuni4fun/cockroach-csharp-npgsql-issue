# Cert issue setting up CockroachDB

I am having a certificate issue that looks like it may be specific to C#/OpenSSL.

My setup is running on Windows 10 with a WSL2 Ubuntu distro. I am running the database and my test code in Docker Desktop's Kubernetes (current/recent). I chose the Kubernetes CA option for both examples.

| tool           | version                  |
| -------------- | ------------------------ |
| Docker Desktop | Community 2.4.0.0 stable |
| Docker Engine  | 19.03.13                 |
| Kubernetes     | v1.18.8                  |

I followed the CockroachDB example linked in each README [here (Rust)](./cockroach-rust-demo/README.md) and [here (C#)](./cockroach-csharp-loader/README.md).

The Rust example worked fine, including the DB dashboard and adding/removing nodes. The C# example is behaving strangely. Using OpenSSL, I converted the *.crt* and *.key* to *.pfx* for client.maxroach.* and it verifies when provided the *ca.crt*.

The problem appears when connecting to the server. It connects, starts the cert verifications, calls *ProvideClientCertificatesCallback()* but does not call *UserCertificateValidationCallback()*.  The error is detailed in the README and indicates that it can't find a file but not which one. I've tried:

- trusting the cert via the main OS cert store,
- directly dropping it in the OpenSSL dir indicated by *openssl version -d* AND also in that path + /certs,
- copying it to various paths that might be referenced,
- verifying that the *.pfx* IS found and decoded (it is found and a wrong password changes path taken)
- googling and binging the error message returned many different ways for hours

I am hoping someone else can chime in on why this example does not work in my containerized, repeatable environment... what's different in the example writer's environment? Also, there are some details that are omitted, like certificate placement relative to code directory or absolute or previous openssl trust steps, if any. I tried a bunch of assumptions but to no avail.

I am currently digging through [this file](https://github.com/npgsql/npgsql/blob/c5812a3a5f274d1dd1700cd5bf8465ded8c00849/src/Npgsql/NpgsqlConnection.cs) to see if I can start piecing this together. I will turn on tracing soon to gather more data. I am posting this to engage anyone who may have the answer and since this will likely warrant a cautionary edit in the Cockroach C# example.

The error message is:

```bash
Unhandled exception. Interop+Crypto+OpenSslCryptographicException: error:2006D080:BIO routines:BIO_new_file:no such file
   at Interop.Crypto.CheckValidOpenSslHandle(SafeHandle handle)
   at Internal.Cryptography.Pal.OpenSslX509CertificateReader.FromFile(String fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
   at System.Security.Cryptography.X509Certificates.X509Certificate..ctor(String fileName, String password, X509KeyStorageFlags keyStorageFlags)       
   at System.Security.Cryptography.X509Certificates.X509Certificate2..ctor(String fileName)
   at Npgsql.NpgsqlConnector.<>c__DisplayClass192_0.<SslRootValidation>b__0(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
   at System.Net.Security.SslStream.UserCertValidationCallbackWrapper(String hostName, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
   at System.Net.Security.SecureChannel.VerifyRemoteCertificate(RemoteCertValidationCallback remoteCertValidationCallback, ProtocolToken& alertToken)  
   at System.Net.Security.SslStream.CompleteHandshake(ProtocolToken& alertToken)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessReceivedBlob(Byte[] buffer, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReadFrame(Byte[] buffer, Int32 readBytes, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartReceiveBlob(Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.StartSendBlob(Byte[] incoming, Int32 count, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ForceAuthentication(Boolean receiveFirst, Byte[] buffer, AsyncProtocolRequest asyncRequest)
   at System.Net.Security.SslStream.ProcessAuthentication(LazyAsyncResult lazyResult, CancellationToken cancellationToken)
   at System.Net.Security.SslStream.AuthenticateAsClient(SslClientAuthenticationOptions sslClientAuthenticationOptions)
   at System.Net.Security.SslStream.AuthenticateAsClient(String targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, Boolean checkCertificateRevocation)
   at Npgsql.NpgsqlConnector.RawOpen(NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
   at Npgsql.NpgsqlConnector.Open(NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
   at Npgsql.ConnectorPool.OpenNewConnector(NpgsqlConnection conn, NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
   at Npgsql.ConnectorPool.<>c__DisplayClass38_0.<<Rent>g__RentAsync|0>d.MoveNext()
--- End of stack trace from previous location where exception was thrown ---
   at Npgsql.NpgsqlConnection.<>c__DisplayClass41_0.<<Open>g__OpenAsync|0>d.MoveNext()
--- End of stack trace from previous location where exception was thrown ---
   at Npgsql.NpgsqlConnection.Open()
   at Cockroach.Program.Simple(String connString) in /usr/src/Program.cs:line 32
   at Cockroach.Program.Main(String[] args) in /usr/src/Program.cs:line 23
   at Cockroach.Program.<Main>(String[] args)
Aborted
```
