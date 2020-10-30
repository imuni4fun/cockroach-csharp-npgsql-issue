use openssl::error::ErrorStack;
use openssl::ssl::{SslConnector, SslFiletype, SslMethod};
use postgres::Client;
use postgres_openssl::MakeTlsConnector;
use std::env;

fn ssl_config() -> Result<MakeTlsConnector, ErrorStack> {
    let mut builder = SslConnector::builder(SslMethod::tls())?;
    builder.set_ca_file("certs/ca.crt")?;
    builder.set_certificate_chain_file("certs/client.maxroach.crt")?;
    builder.set_private_key_file("certs/client.maxroach.key", SslFiletype::PEM)?;
    Ok(MakeTlsConnector::new(builder.build()))
}

pub fn basic_example() {
    let connector = ssl_config().unwrap();
    let host = env::var("COCKROACH_HOST").expect("must provide env var 'COCKROACH_HOST'");
    let mut client = Client::connect(
        &format!("postgresql://maxroach@{}:26257/bank", host),
        connector,
    )
    .unwrap();

    // Create the "accounts" table.
    client
        .execute(
            "CREATE TABLE IF NOT EXISTS accounts (id INT PRIMARY KEY, balance INT)",
            &[],
        )
        .unwrap();


    // Print out the balances.
    println!("Initial balances:");
    for row in &client
        .query("SELECT id, balance FROM accounts", &[])
        .unwrap()
    {
        let id: i64 = row.get(0);
        let balance: i64 = row.get(1);
        println!("{} {}", id, balance);
    }

    // Insert two rows into the "accounts" table.
    client
        .execute(
            "INSERT INTO accounts (id, balance) VALUES (1, 1000), (2, 250)",
            &[],
        )
        .unwrap();

    // Print out the balances.
    println!("Updated balances:");
    for row in &client
        .query("SELECT id, balance FROM accounts", &[])
        .unwrap()
    {
        let id: i64 = row.get(0);
        let balance: i64 = row.get(1);
        println!("{} {}", id, balance);
    }
}
