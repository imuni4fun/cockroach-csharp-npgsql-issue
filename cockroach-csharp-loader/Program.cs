using System;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Npgsql;
using System.Threading.Tasks;

namespace Cockroach
{
    class Program
    {
        const string COCKROACH_CERT_PASS = "password";
        const string COCKROACH_HOST = "cockroachdb-public.default.svc.cluster.local";

        static async Task Main(string[] args)
        {
            var connStringBuilder = new NpgsqlConnectionStringBuilder();
            connStringBuilder.Host = COCKROACH_HOST;
            connStringBuilder.Port = 26257;
            connStringBuilder.SslMode = SslMode.Require;
            connStringBuilder.Username = "maxroach";
            connStringBuilder.Database = "bank";
            await Simple(connStringBuilder.ConnectionString);
        }

        static async Task Simple(string connString)
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.ProvideClientCertificatesCallback += ProvideClientCertificatesCallback;
                conn.UserCertificateValidationCallback += UserCertificateValidationCallback;
                conn.Open(); // CRASHES HERE WITH ERROR SHOWN AT END OF FILE

                // clear previous "accounts" table, if any.
                await new NpgsqlCommand("DROP TABLE EXISTS accounts", conn).ExecuteNonQueryAsync();
                // Create the "accounts" table.
                await new NpgsqlCommand("CREATE TABLE IF NOT EXISTS accounts (id INT PRIMARY KEY, balance INT)", conn).ExecuteNonQueryAsync();

                // Insert two rows into the "accounts" table.
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "UPSERT INTO accounts(id, balance) VALUES(@id1, @val1), (@id2, @val2)";
                    cmd.Parameters.AddWithValue("id1", 1);
                    cmd.Parameters.AddWithValue("val1", 2000);
                    cmd.Parameters.AddWithValue("id2", 2);
                    cmd.Parameters.AddWithValue("val2", 3250);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Print out the balances.
                System.Console.WriteLine("Initial balances:");
                using (var cmd = new NpgsqlCommand("SELECT id, balance FROM accounts", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        Console.Write("\taccount {0}: {1}\n", reader.GetValue(0), reader.GetValue(1));
            }
        }

        static void ProvideClientCertificatesCallback(X509CertificateCollection clientCerts)
        {
            // To be able to add a certificate with a private key included, we must convert it to
            // a PKCS #12 format. The following openssl command does this:
            // openssl pkcs12 -password pass: -inkey client.maxroach.key -in client.maxroach.crt -export -out client.maxroach.pfx
            // As of 2018-12-10, you need to provide a password for this to work on macOS.
            // See https://github.com/dotnet/corefx/issues/24225

            // Note that the password used during X509 cert creation below
            // must match the password used in the openssl command above.
            // clientCerts.Add(new X509Certificate2("certs/client.maxroach.pfx", COCKROACH_CERT_PASS));
            var cert_file = Environment.GetEnvironmentVariable("COCKROACH_CERT_FILE");
            var cert_pass = Environment.GetEnvironmentVariable("COCKROACH_CERT_PASS");
            Console.WriteLine($"Reading from COCKROACH_CERT_FILE: {cert_file}");
            Console.WriteLine($"Using password from COCKROACH_CERT_PASS: {cert_pass}");
            var bytes = System.IO.File.ReadAllBytes(cert_file); //"/cockroach-certs/client.maxroach.pfx"
            Console.WriteLine($"Total certificate bytes read: {bytes.Length}");
            var new_cert = new X509Certificate2(bytes, cert_pass);
            Console.WriteLine($"Detected cert algorithm: {new_cert.GetKeyAlgorithm()}");
            clientCerts.Add(new_cert);
            Console.WriteLine("Added cert to clientCerts");
            // EXECUTION COMPLETES (SEEMINGLY SUCCESSFULLY)
        }

        // By default, .Net does all of its certificate verification using the system certificate store.
        // This callback is necessary to validate the server certificate against a CA certificate file.
        static bool UserCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain defaultChain, SslPolicyErrors defaultErrors)
        {
            // THIS IS NEVER EXECUTED!!
            var cert_file = Environment.GetEnvironmentVariable("COCKROACH_CA_FILE");
            Console.WriteLine($"Reading from COCKROACH_CA_FILE: {cert_file}");
            var bytes = System.IO.File.ReadAllBytes(cert_file);
            Console.WriteLine($"Total CA certificate bytes read: {bytes.Length}");
            var new_cert = new X509Certificate2(bytes);
            Console.WriteLine($"Detected CA cert algorithm: {new_cert.GetKeyAlgorithm()}");

            X509Certificate2 caCert = new_cert;

            // X509Certificate2 caCert = new X509Certificate2("/cockroach-certs/ca.crt");
            X509Chain caCertChain = new X509Chain();
            caCertChain.ChainPolicy = new X509ChainPolicy()
            {
                RevocationMode = X509RevocationMode.NoCheck,
                RevocationFlag = X509RevocationFlag.EntireChain
            };
            Console.WriteLine("A");
            caCertChain.ChainPolicy.ExtraStore.Add(caCert);

            Console.WriteLine("B");
            X509Certificate2 serverCert = new X509Certificate2(certificate);

            Console.WriteLine("C");
            caCertChain.Build(serverCert);
            if (caCertChain.ChainStatus.Length == 0)
            {
                Console.WriteLine("D");
                // No errors
                return true;
            }

            foreach (X509ChainStatus status in caCertChain.ChainStatus)
            {
                Console.WriteLine("E");
                // Check if we got any errors other than UntrustedRoot (which we will always get if we don't install the CA cert to the system store)
                if (status.Status != X509ChainStatusFlags.UntrustedRoot)
                {
                    Console.WriteLine("F");
                    return false;
                }
            }
            Console.WriteLine("G");
            return true;
        }

    }
}


/*
root@demo-cs:/usr/src# ./bin/Debug/netcoreapp3.1/cockroach-csharp-loader
Reading from COCKROACH_CERT_FILE: client.maxroach.pfx
Using password from COCKROACH_CERT_PASS: password
Total certificate bytes read: 2365
Detected cert algorithm: 1.2.840.113549.1.1.1
Added cert to clientCerts
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
*/