# C# loader

Start from [previous working example](../cockroach-rust-demo/README.md) using K8s CA for certificate signing.

```bash
# run this inside pod to convert PEM format to PFX
openssl pkcs12 -inkey client.maxroach.key -password pass:$COCKROACH_CERT_PASS -in client.maxroach.crt -export -out client.maxroach.pfx
# during debugging also tried exporting chain via '-chain' and saw 50% larger pfx file
openssl pkcs12 -inkey client.maxroach.key -password pass:$COCKROACH_CERT_PASS -in client.maxroach.crt -export -chain -out client.maxroach.pfx
```

```bash
DOCKER_BUILDKIT=1 docker build --progress=plain -t cockroach-csharp-loader:latest .
kubectl apply -f demo-cs.yaml
# run 'kubectl get csr' and 'kubectl certificate approve <csr>' if not previously approved
# fix certs (generate .pfx file, trust ca.crt), this must be run AFTER init container populates the emptydir at /cockroach-certs
kubectl exec -it pod/demo-cs -- bash
# the next commands are run inside the container
./fix-certs.sh
./bin/Debug/netcoreapp3.1/cockroach-csharp-loader
# The c# app executed here errors out looking for a file but can't rationalize it. It DOES load the client.maxroach.pfx
# and changing the password to not match indicates bad password. It never calls the code that is provided the "ca.crt"
# cert file path so it must be looking elsewhere. Openssl is using "/usr/local/ssl/certs/" for its store and I've sent
# the ca.crt there... does the name matter?
```

Tried several combinations including trusting CA (official dotnet sdk debian-buster-slim container, see Dockerfile):

```bash
cp /cockroach-certs/* /usr/local/share/ca-certificates/
update-ca-certificates
cp /var/run/secrets/kubernetes.io/serviceaccount/ca.crt /usr/local/share/ca-certificates/
update-ca-certificates
```

... and verified that PFX cert is being found via prinouts and trying bad password to see error change. Also verified that *UserCertificateValidationCallback()* delegate is never being called. Get this error message always:

```bash
redacted@RyzenRig:/mnt/c/GitHub/cockroach-csharp-npgsql-issue/cockroach-csharp-loader$ kubectl apply -f demo-cs.yaml 
pod/demo-cs created
redacted@RyzenRig:/mnt/c/GitHub/cockroach-csharp-npgsql-issue/cockroach-csharp-loader$ kubectl exec -it pod/demo-cs -- bash
root@demo-cs:/usr/src# ./fix_cert.sh 
Updating certificates in /etc/ssl/certs...
1 added, 0 removed; done.
Running hooks in /etc/ca-certificates/update.d...
done.
root@demo-cs:/usr/src# ./bin/Debug/netcoreapp3.1/cockroach-csharp-loader 
Reading from COCKROACH_CERT_FILE: /cockroach-certs/client.maxroach.pfx
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
root@demo-cs:/usr/src# ll certs/
total 20
drwxrwxrwx 2 root root 4096 Oct 29 22:15 .
drwxr-xr-x 1 root root 4096 Oct 29 22:14 ..
lrwxrwxrwx 1 root root   52 Oct 29 22:14 ca.crt -> /var/run/secrets/kubernetes.io/serviceaccount/ca.crt
-rw-r--r-- 1 root root 1094 Oct 29 22:14 client.maxroach.crt
-r-------- 1 root root 1679 Oct 29 22:14 client.maxroach.key
-rw------- 1 root root 2365 Oct 29 22:15 client.maxroach.pfx
root@demo-cs:/usr/src# ll /cockroach-certs/
total 20
drwxrwxrwx 2 root root 4096 Oct 29 22:15 .
drwxr-xr-x 1 root root 4096 Oct 29 22:14 ..
lrwxrwxrwx 1 root root   52 Oct 29 22:14 ca.crt -> /var/run/secrets/kubernetes.io/serviceaccount/ca.crt
-rw-r--r-- 1 root root 1094 Oct 29 22:14 client.maxroach.crt
-r-------- 1 root root 1679 Oct 29 22:14 client.maxroach.key
-rw------- 1 root root 2365 Oct 29 22:15 client.maxroach.pfx
root@demo-cs:/usr/src# ll certs 
lrwxrwxrwx 1 root root 16 Oct 29 22:11 certs -> /cockroach-certs
root@demo-cs:/usr/src# openssl verify -verbose -show_chain -CAfile certs/ca.crt certs/client.maxroach.crt
certs/client.maxroach.crt: OK
Chain:
depth=0: O = Cockroach, CN = maxroach (untrusted)
depth=1: CN = kubernetes
root@demo-cs:/usr/src#
```

and updated script to print out more cert info

```bash
root@demo-cs:/usr/src# ./fix_certs.sh 
certs located before running this script
/cockroach-certs/client.maxroach.key
/cockroach-certs/client.maxroach.crt
/cockroach-certs/ca.crt
certs/client.maxroach.key
certs/client.maxroach.crt
certs/ca.crt
/usr/lib/ssl/certs/ca-certificates.crt
/usr/lib/ssl/certs/ca-certificates.crt
Updating certificates in /etc/ssl/certs...
rehash: warning: skipping duplicate certificate in ca.crt
2 added, 0 removed; done.
Running hooks in /etc/ca-certificates/update.d...
done.
certs sprinkled about to several locations (some may have been present before running this script)
/cockroach-certs/client.maxroach.key
/cockroach-certs/client.maxroach.pfx
/cockroach-certs/client.maxroach.crt
/cockroach-certs/ca.crt
certs/client.maxroach.key
certs/client.maxroach.pfx
certs/client.maxroach.crt
certs/ca.crt
/usr/lib/ssl/certs/ca-certificates.crt
/usr/lib/ssl/certs/ca.crt
/usr/lib/ssl/ca.crt
/usr/lib/ssl/certs/ca-certificates.crt
/usr/lib/ssl/certs/ca.crt
```
