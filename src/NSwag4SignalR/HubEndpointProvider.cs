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

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace NSwag4SignalR;

internal sealed class HubEndpointProvider {

    private readonly EndpointDataSource _endpointDataSource;

    public HubEndpointProvider(EndpointDataSource endpointDataSource) {
        _endpointDataSource = endpointDataSource;
    }

    public IEnumerable<(Type HubType, string Path)> GetHubEndpoints() {
        foreach (var endpoint in _endpointDataSource.Endpoints) {
            var routeEndpoint = endpoint as RouteEndpoint;
            if (routeEndpoint != null) {
                var metadata = routeEndpoint.Metadata;

                // Exlcude the negotiate endpoint
                if (metadata.GetMetadata<NegotiateMetadata>() is not null) {
                    continue;
                }

                var hubMetadata = metadata.GetMetadata<HubMetadata>();
                if (hubMetadata != null) {
                    var hubType = hubMetadata.HubType;
                    var pattern = routeEndpoint.RoutePattern.RawText ?? string.Empty;
                                           
                    yield return (HubType: hubType, Path: pattern);
                }
            }
        }
    }
}