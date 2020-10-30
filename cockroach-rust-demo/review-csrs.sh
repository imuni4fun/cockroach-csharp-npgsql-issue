#!/bin/bash

for csr_name in $(kubectl get csr --output=json | jq '.items[].metadata.name' | sed 's/"//g')
do
    kubectl describe csr $csr_name
    read -p "approve CSR? (y/n) : " approve
    if [[ "$approve" == "y" ]]
    then
        kubectl certificate approve $csr_name
    fi
done
kubectl get csr
