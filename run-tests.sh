VERSION=1.0.0
GIT_SHA=$(git rev-parse --short HEAD)

docker build --build-arg VERSION="${VERSION}" --build-arg GIT_SHA="${GIT_SHA}" --progress=plain --target=tests --tag=nswag4signalr.tests:latest --file=docker/Dockerfile . 

docker container create --name test -t nswag4signalr.tests
docker start test
docker cp test:/app/coverage-report.zip coverage-report.zip
docker stop test
docker rm test