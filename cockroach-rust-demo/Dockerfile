FROM rust:1.47.0-slim-buster

RUN apt-get update && \
    apt-get install -y libssl-dev pkg-config && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /usr/src/
COPY . .
RUN cargo build
# RUN cargo build --release
# RUN cargo install --path .

RUN echo "alias ll='ls -la'" >> /root/.bashrc
RUN ln -s /cockroach-certs certs
CMD sleep 10000
