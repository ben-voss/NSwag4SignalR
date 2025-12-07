# NSwag4SignalR

NSwag4SignalR extends OpenAPI/SwaggerUI to describe SignalR (real-time) endpoints alongside regular HTTP APIs. It provides document processors and helpers to include Hub endpoints and SignalR semantics in generated OpenAPI documents and the Swagger UI.

## Features

- Adds SignalR Hub endpoints to generated OpenAPI documents.
- Integrates with NSwag.AspNetCore and Swagger UI.
- Small, dependency-light library intended for .NET 10+ projects

## Installation

From NuGet:

```bash
dotnet add package NSwag4SignalR
```

## Quick start

In Program.cs (minimal API / .NET 10+):
```csharp
using NSwag4SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR and REST controllers as normal
builder.Services.AddSignalR();
builder.Services.AddControllers();

// Add Swagger services
builder.Services.AddOpenApiDocument();

// Add the NSwag4SignalR extension
builder.Services.AddNSwag4SignalR();

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi(swaggerUiSettings => {
    // Use the NSwag4SignalR extension
    app.UseNSwag4SignalR(swaggerUiSettings);
});

app.UseEndpoints(endpoints => {
    endpoints.MapControllers();

    // Redirect / to /swagger
    endpoints.MapGet("/", context => {
        context.Response.Redirect("/swagger");
        return Task.CompletedTask;
    });

    endpoints.MapHub<MyHub>("/hubs/myhub");
});

app.Run();
```

## Contributing
- Fork, create a branch, open a PR.
- Follow the existing code style and add small, focused commits.

## License
MIT — see LICENSE

## Contact / Repo
https://github.com/ben-voss/NSwag4SignalR