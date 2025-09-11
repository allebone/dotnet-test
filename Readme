###How Do I build?

Run the Generate Containers Bash Script:
bash ./generate_dockers.sh

###How do I test?

docker run --rm \
  -e KEYCLOAK_BASE_URL="https://identity.omni.af.mil" \
  -e KEYCLOAK_REALM="OMNI" \
  registry.omni.mil/dotnet_test/dotnet-test:ms

docker run --rm \
  -e KEYCLOAK_BASE_URL="https://identity.omni.af.mil" \
  -e KEYCLOAK_REALM="OMNI" \
  registry.omni.mil/dotnet_test/dotnet-test:ib

docker run --rm \
  -e KEYCLOAK_BASE_URL="https://identity.omni.af.mil" \
  -e KEYCLOAK_REALM="OMNI" \
  registry.omni.mil/dotnet_test/dotnet-test:cg

###How does it Compare?

Check the Newly Uploaded Containers in Harbor, Compare Number of Trivvy Vulnerabilities and Container Size.
