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

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSwag.Generation.Processors;
using Shouldly;

namespace NSwag4SignalR.Tests;

public sealed class ServiceCollectionExtensionsTests {

    [Fact]
    public void AddNSwag4SignalRRegistersWebSocketDocumentProcessor() {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<EndpointDataSource>());

        services.AddNSwag4SignalR();
        var serviceProvider = services.BuildServiceProvider();

        var processor = serviceProvider.GetService<IDocumentProcessor>();
        processor.ShouldNotBeNull();
        processor.ShouldBeOfType<WebSocketDocumentProcessor>();
    }

    [Fact]
    public void AddNSwag4SignalRRegistersHubEndpointProvider() {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<EndpointDataSource>());

        services.AddNSwag4SignalR();
        var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider.GetService<HubEndpointProvider>();
        provider.ShouldNotBeNull();
    }

    [Fact]
    public void AddNSwag4SignalRRegistersServicesAsSingletons() {
        var services = new ServiceCollection();

        services.AddNSwag4SignalR();

        var processorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IDocumentProcessor));
        processorDescriptor.ShouldNotBeNull();
        processorDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        var providerDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(HubEndpointProvider));
        providerDescriptor.ShouldNotBeNull();
        providerDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddNSwag4SignalRReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddNSwag4SignalR();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddNSwag4SignalRMultipleCallsRegisterSingleInstances() {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<EndpointDataSource>());

        services.AddNSwag4SignalR();
        services.AddNSwag4SignalR();
        var serviceProvider = services.BuildServiceProvider();

        var processors = serviceProvider.GetServices<IDocumentProcessor>().ToList();
        processors.Count.ShouldBe(1);

        var providers = serviceProvider.GetServices<HubEndpointProvider>().ToList();
        providers.Count.ShouldBe(1);
    }
}