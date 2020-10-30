FROM mcr.microsoft.com/dotnet/core/sdk:3.1.403-buster
# FROM mcr.microsoft.com/dotnet/core/runtime:3.1.9-buster-slim

RUN apt-get update && \
    apt-get install -y libssl-dev pkg-config && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /usr/src/
COPY . .
RUN dotnet build
# RUN cargo build --release
# RUN cargo install --path .

RUN echo "alias ll='ls -la'" >> /root/.bashrc
RUN ln -s /cockroach-certs certs
RUN echo '# initializing empty script' > fix_certs.sh && \
    echo 'echo "certs located before running this script"' >> fix_certs.sh && \
    echo 'find -L $(echo "/cockroach-certs";echo "certs";echo "/usr/lib/ssl/";echo "/usr/lib/ssl/certs") | grep -e "\.crt" -e "\.pfx" -e "\.key"' >> fix_certs.sh && \
    echo 'openssl pkcs12 -inkey certs/client.maxroach.key -password pass:$COCKROACH_CERT_PASS -in certs/client.maxroach.crt -export -out certs/client.maxroach.pfx' >> fix_certs.sh && \
    echo 'mkdir /usr/local/share/ca-certificates/k8s' >> fix_certs.sh && \
    echo '# yes, the following is excessive' >> fix_certs.sh && \
    echo 'cp /cockroach-certs/ca.crt /usr/local/share/ca-certificates/k8s/ca.crt' >> fix_certs.sh && \
    echo 'cp /cockroach-certs/ca.crt /usr/local/share/ca-certificates/ca.crt' >> fix_certs.sh && \
    echo 'cp /cockroach-certs/ca.crt /usr/lib/ssl/certs/ca.crt' >> fix_certs.sh && \
    echo 'cp /cockroach-certs/ca.crt /usr/lib/ssl/ca.crt' >> fix_certs.sh && \
    echo 'update-ca-certificates' >> fix_certs.sh && \
    echo 'echo "certs sprinkled about to several locations (some may have been present before running this script)"' >> fix_certs.sh && \
    echo 'find -L $(echo "/cockroach-certs";echo "certs";echo "/usr/lib/ssl/";echo "/usr/lib/ssl/certs") | grep -e "\.crt" -e "\.pfx" -e "\.key"' >> fix_certs.sh && \
    chmod a+x fix_certs.sh

ENV COCKROACH_CERT_FILE=/cockroach-certs/client.maxroach.pfx
ENV COCKROACH_CA_FILE=/cockroach-certs/ca.crt
ENV COCKROACH_CERT_PASS=password
CMD ./fix_certs.sh && sleep 10000
