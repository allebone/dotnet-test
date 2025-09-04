#!/usr/bin/env bash
set -euo pipefail

# Registry and project path
REGISTRY="registry.omni.mil"
PROJECT="dotnet-test"
IMAGE="dotnet-test"

# List of variants (Dockerfiles must be named dotnet-MS, dotnet-IB, dotnet-CG)
VARIANTS=("MS" "IB" "CG")

# Make sure we are logged in
echo "Logging into $REGISTRY..."
docker login "$REGISTRY"

for variant in "${VARIANTS[@]}"; do
  DOCKERFILE="dotnet-$variant"
  TAG=$(echo "$variant" | tr '[:upper:]' '[:lower:]')   # ms, ib, cg

  FULL_IMAGE="$REGISTRY/$PROJECT/$IMAGE:$TAG"

  echo "======================================"
  echo " Building and pushing $FULL_IMAGE"
  echo " Using Dockerfile: $DOCKERFILE"
  echo "======================================"

  docker build -f "$DOCKERFILE" -t "$FULL_IMAGE" .
  docker push "$FULL_IMAGE"
done

echo "✅ All images built and pushed successfully."
