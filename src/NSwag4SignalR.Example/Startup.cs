// Copyright 2025 Ben Voß
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using NSwag;
using NSwag4SignalR.Example.Hubs;

namespace NSwag4SignalR.Example;

public class Startup {
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment) {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public void ConfigureServices(IServiceCollection services) {
        services.AddSignalR();

        services
            .AddControllers()
            .AddControllersAsServices();

        // Add Swagger services
        services.AddOpenApiDocument(options => {
            options.PostProcess = document => {
                document.Info = new OpenApiInfo {
                    Version = "v1",
                    Title = "Example API",
                    Description = "NSwag 4 SignalR Example"
                };
            };
        });
        
        // Add the NSwag4SignalR extension
        services.AddNSwag4SignalR();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        app.UseRouting();

        if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        }

        // Enable Swagger middleware
        app
            .UseOpenApi()
            .UseSwaggerUi(swaggerUiSettings =>
            {
                swaggerUiSettings.EnableTryItOut = true;

                // Use the NSwag4SignalR extension
                app.UseNSwag4SignalR(swaggerUiSettings);
            });

        app.UseStaticFiles();

        app.UseEndpoints(endpoints => {
            endpoints.MapControllers();

            // Redirect / to /swagger
            endpoints.MapGet("/", context => {
                context.Response.Redirect("/swagger");
                return Task.CompletedTask;
            });

            endpoints.MapHub<StronglyTypedHub>("/ws/strong");
            endpoints.MapHub<WeakTypedHub>("/ws/weak");
        });
    }
}
