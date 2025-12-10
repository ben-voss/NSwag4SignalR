
dotnet test tests/NSwag4SignalR.Tests/NSwag4SignalR.Tests.csproj --no-build --collect:"XPlat Code Coverage"

reportgenerator -reports:"**/TestResults/*/coverage.cobertura.xml" -targetdir:"/app/coverage-report" -reporttypes:Html

cd /app/coverage-report
zip -r -q /app/coverage-report.zip ./*
