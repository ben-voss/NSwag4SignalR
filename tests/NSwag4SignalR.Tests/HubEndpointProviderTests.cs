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
using Shouldly;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;

namespace NSwag4SignalR.Tests;

public sealed class HubEndpointProviderTests {
    private sealed class TestHub : Hub { }
    
    private sealed class AnotherTestHub : Hub { }

    [Fact]
    public void GetHubEndpointsReturnsHubEndpoints() {
        var hubMetadata = new EndpointMetadataCollection(new HubMetadata(typeof(TestHub)));
        var hubEndpoint = new RouteEndpoint(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse("/hubs/test"),
            order: 0,
            metadata: hubMetadata,
            displayName: "TestHub"
        );

        var mockDataSource = new Mock<EndpointDataSource>(); 
        mockDataSource.SetupGet(x => x.Endpoints).Returns([hubEndpoint]);
        var provider = new HubEndpointProvider(mockDataSource.Object);

        var results = provider.GetHubEndpoints().ToList();

        results.Count.ShouldBe(1);
        results[0].HubType.ShouldBe(typeof(TestHub));
        results[0].Path.ShouldBe("/hubs/test");
    }

    [Fact]
    public void GetHubEndpointsExcludesNegotiateEndpoints() {
        var hubMetadata = new EndpointMetadataCollection(
            new HubMetadata(typeof(TestHub)),
            new NegotiateMetadata()
        );
        var negotiateEndpoint = new RouteEndpoint(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse("/hubs/test/negotiate"),
            order: 0,
            metadata: hubMetadata,
            displayName: "TestHub-Negotiate"
        );

        var mockDataSource = new Mock<EndpointDataSource>();
        mockDataSource.SetupGet(x => x.Endpoints).Returns([negotiateEndpoint]);
        var provider = new HubEndpointProvider(mockDataSource.Object);

        var results = provider.GetHubEndpoints().ToList();

        results.ShouldBeEmpty();
    }

    [Fact]
    public void GetHubEndpointsIgnoresNonRouteEndpoints() {
        var nonRouteEndpoint = new Endpoint(
            requestDelegate: _ => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(),
            displayName: "NonRoute"
        );

        var mockDataSource = new Mock<EndpointDataSource>();
        mockDataSource.SetupGet(x => x.Endpoints).Returns([nonRouteEndpoint]);
        var provider = new HubEndpointProvider(mockDataSource.Object);

        var results = provider.GetHubEndpoints().ToList();

        results.ShouldBeEmpty();
    }

    [Fact]
    public void GetHubEndpointsReturnsMultipleHubs() {
        var hub1Metadata = new EndpointMetadataCollection(new HubMetadata(typeof(TestHub)));
        var hub1Endpoint = new RouteEndpoint(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse("/hubs/test1"),
            order: 0,
            metadata: hub1Metadata,
            displayName: "TestHub1"
        );

        var hub2Metadata = new EndpointMetadataCollection(new HubMetadata(typeof(AnotherTestHub)));
        var hub2Endpoint = new RouteEndpoint(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse("/hubs/test2"),
            order: 1,
            metadata: hub2Metadata,
            displayName: "TestHub2"
        );

        var mockDataSource = new Mock<EndpointDataSource>();
        mockDataSource.SetupGet(x => x.Endpoints).Returns([hub1Endpoint, hub2Endpoint]);
        var provider = new HubEndpointProvider(mockDataSource.Object);

        var results = provider.GetHubEndpoints().ToList();

        results.Count.ShouldBe(2);
        results[0].HubType.ShouldBe(typeof(TestHub));
        results[0].Path.ShouldBe("/hubs/test1");
        results[1].HubType.ShouldBe(typeof(AnotherTestHub));
        results[1].Path.ShouldBe("/hubs/test2");
    }

    [Fact]
    public void GetHubEndpointsHandlesNullRawText() {
        var hubMetadata = new EndpointMetadataCollection(new HubMetadata(typeof(TestHub)));
        var pattern = RoutePatternFactory.Parse("/hubs/test");
        var hubEndpoint = new RouteEndpoint(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: pattern,
            order: 0,
            metadata: hubMetadata,
            displayName: "TestHub"
        );

        var mockDataSource = new Mock<EndpointDataSource>();
        mockDataSource.SetupGet(x => x.Endpoints).Returns([hubEndpoint]);
        var provider = new HubEndpointProvider(mockDataSource.Object);

        var results = provider.GetHubEndpoints().ToList();

        results.Count.ShouldBe(1);
        results[0].Path.ShouldNotBeNullOrEmpty();
    }
}