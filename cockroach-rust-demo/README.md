# CockroachDB Rust demo

## Set up CockroachDB

https://www.cockroachlabs.com/docs/stable/build-a-rust-app-with-cockroachdb.html

### Install CockroachDB securely

https://www.cockroachlabs.com/docs/v20.1/orchestrate-a-local-cluster-with-kubernetes#manual

https://www.cockroachlabs.com/docs/v20.1/authentication#using-digital-certificates-with-cockroachdb

### Deploy instance

```bash
curl -O https://raw.githubusercontent.com/cockroachdb/cockroach/master/cloud/kubernetes/cockroachdb-statefulset-secure.yaml
kubectl create -f cockroachdb-statefulset-secure.yaml
# kubectl get csr
# kubectl certificate approve <csr_name>
./review-csrs.sh
# one-time init of job to join nodes into one cluster
curl -O https://raw.githubusercontent.com/cockroachdb/cockroach/master/cloud/kubernetes/cluster-init-secure.yaml
kubectl create -f cluster-init-secure.yaml
kubectl get csr
kubectl certificate approve default.client.root
# verify job and pods ready
kubectl get job cluster-init-secure
kubectl get pods
```

### Connect via secure client

Use this for all sql transactions, may need to change user

```bash
curl -O https://raw.githubusercontent.com/cockroachdb/cockroach/master/cloud/kubernetes/client-secure.yaml
kubectl create -f client-secure.yaml
kubectl exec -it cockroachdb-client-secure -- ./cockroach sql --certs-dir=/cockroach-certs --host=cockroachdb-public
```

### Run commands in the DB to configure

```sql
CREATE DATABASE bank;
CREATE TABLE bank.accounts (id INT PRIMARY KEY, balance DECIMAL);
INSERT INTO bank.accounts VALUES (1, 1000.50);
SELECT * FROM bank.accounts;
CREATE USER roach WITH PASSWORD 'Q7gc8rEdS';
```

```bash
# optional, can leave running (security vulnerability) or recreate when needed
kubectl delete pod cockroachdb-client-secure
```

### Grant access for Admin UI

```sql
GRANT admin TO roach;
```

### Access the Web portal

```bash
kubectl port-forward cockroachdb-0 8080
```

Visit portal: https://localhost:8080

### Check node status

```bash
kubectl exec -it cockroachdb-client-secure -- ./cockroach node status --certs-dir=/cockroach-certs --host=cockroachdb-public
```

### Simulate node failure

```bash
kubectl delete pod cockroachdb-1
```

### Add nodes

```bash
kubectl scale statefulset cockroachdb --replicas=5
kubectl get csr
./review-csrs.sh
# kubectl certificate approve default.node.cockroachdb-3
# kubectl certificate approve default.node.cockroachdb-4
# confirm ready state
kubectl get pods
```

### Remove (decommission) nodes

**ATTENTION: You MUST tell the DB before reducing replicas. This lets in-flight requests complete, rejects new request, and transfers range replicas (to rebalance). You MUST decommission the HIGHEST-numbered nodes.**

https://www.cockroachlabs.com/docs/v20.1/remove-nodes

```bash
kubectl exec -it cockroachdb-client-secure -- ./cockroach node decommission 5 --certs-dir=/cockroach-certs --host=cockroachdb-public
kubectl scale statefulset cockroachdb --replicas=4
```

## Create Rust application

### Add dependencies

```toml
[dependencies]
postgres = "0.18.1"
openssl = "0.10.30"
postgres-openssl = "0.4.0"
```

### Create user and table

```sql
-- from secure client pod
CREATE USER IF NOT EXISTS maxroach;
CREATE DATABASE bank;
-- sets default permissions for future tables in this database for the given user
GRANT ALL ON DATABASE bank TO maxroach;
-- if the database already existed, must also grant permissions to existing tables for this user
GRANT ALL ON TABLE bank.* TO maxroach;
```

**CANNOT DO THIS NEXT STEP (GENERATING CLIENT CERTS) UNLESS CREATED CA CERTS MANUALLY** or must find some other way to obtain ca.key (CA private key). Without ca.key, MUST use user/password instead.

**CAN GET AROUND THIS BY USING INIT CONTAINER TO GENERATE AND MOUNT CERTS**.  See updated *demo.yaml* file. Must approve CSR via *kubectl*.

```bash
# create client cert for user (ONLY if we have ca.key)
# from secure client
kubectl exec -it cockroachdb-client-secure -- ./cockroach cert create-client maxroach --certs-dir=/cockroach-certs --ca-key=secure-location/ca.key
# not needed if using K8s CA and init container
```

### Containerize

```bash
# build container
DOCKER_BUILDKIT=1 docker build --progress=plain -t cockroach-rust-demo:latest .
# create a test container and connect into it
# kubectl run --image=cockroach-rust-demo:latest --image-pull-policy=Never demo sleep 1000 && \
#   sleep 1 && \
#   kubectl exec -it pod/demo -- bash
kubectl apply -f demo.yaml
# interactive execution
kubectl exec -it pod/demo -- bash
# inside container
# expected previously configured:
#   (from Dockerfile) ln -s /var/run/secrets/kubernetes.io/serviceaccount/ certs
#   (from YAML) export COCKROACH_HOST=cockroachdb-public.default.svc.cluster.local
./target/debug/cockroach-rust-demo
# automatic execution
kubectl exec -it pod/demo -- ./target/debug/cockroach-rust-demo
```
