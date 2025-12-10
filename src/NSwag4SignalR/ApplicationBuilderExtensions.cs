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

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using NSwag.AspNetCore;

namespace NSwag4SignalR;

/// <summary>
/// Extensions for using NSwag4SignalR components for modifying SwaggerUI to operate on the SignalR components of an OpenAPI document.
/// </summary>
public static class SwaggerUiSettingsExtensions {
    /// <summary>
    /// Uses NSwag4SignalR components for modifying SwaggerUI to operate on the SignalR components of an OpenAPI document.
    /// </summary>
    public static SwaggerUiSettings UseNSwag4SignalR(this IApplicationBuilder app, SwaggerUiSettings settings) {
        settings.CustomJavaScriptPath = new PathString(settings.Path).Add("/swaggerui-4-signalr.js").Value;

        app.UseFileServer(new FileServerOptions
        {
            RequestPath = new PathString(settings.Path),
            FileProvider = new EmbeddedFileProvider(typeof(SwaggerUiSettingsExtensions).GetTypeInfo().Assembly, nameof(NSwag4SignalR))
        });

        return settings;
    }
}
