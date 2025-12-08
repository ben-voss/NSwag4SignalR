VERSION=1.0.0
GIT_SHA=$(git rev-parse --short HEAD)

docker build --build-arg VERSION="${VERSION}" --build-arg GIT_SHA="${GIT_SHA}" --progress=plain --target=pack --tag=nswag4signalr.pack:latest --file=docker/Dockerfile . 