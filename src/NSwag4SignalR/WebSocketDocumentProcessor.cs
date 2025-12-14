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
using Microsoft.AspNetCore.SignalR;
using Namotion.Reflection;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Runtime.CompilerServices;

namespace NSwag4SignalR;

internal partial class WebSocketDocumentProcessor : IDocumentProcessor {
    [System.Text.RegularExpressions.GeneratedRegex("(?<!^)([A-Z])")]
    private static partial System.Text.RegularExpressions.Regex HubNameRegex();

    private readonly IHubEndpointProvider _hubEndpointProvider;

    public WebSocketDocumentProcessor(IHubEndpointProvider hubEndpointProvider)
        => _hubEndpointProvider = hubEndpointProvider;

    private static string MakeFriendlyHubName(string hubTypeName) {
        // Remove the "Hub" suffix if present
        if (hubTypeName.EndsWith("Hub") && hubTypeName.Length > 3) {
            hubTypeName = hubTypeName[..^3];
        }

        // Add spaces before capital letters (except the first letter)
        return HubNameRegex().Replace(hubTypeName, " $1");
    }

    private static IEnumerable<(string Action, MethodInfo)> GetHubMethods(Type hubType) {
        // Look for a strongly typed Hub<T> and iterate the members of T to add more detailed documentation if needed
        if (hubType.BaseType!.IsGenericType && hubType.BaseType.GetGenericTypeDefinition() == typeof(Hub<>)) {
            var genericArg = hubType.BaseType.GetGenericArguments()[0];
            foreach (var method in genericArg.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                if (IsHubMethod(method)) {
                    yield return (OpenApiOperationMethod.Get, method);
                }
            }

            foreach (var method in GetWeakHubMethods(hubType)) {
                if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)) {
                    yield return (OpenApiOperationMethod.Put, method);
                } else {
                    yield return (OpenApiOperationMethod.Post, method);
                }
            }
        } else {
            // Use the public methods on the hub itself
            foreach (var method in GetWeakHubMethods(hubType)) {
                yield return (OpenApiOperationMethod.Post, method);
            }
        }
    }

    public static IEnumerable<MethodInfo> GetWeakHubMethods(Type hubType) {
        // Get all public instance methods that are not part of IDisposable and are considered hub methods
        var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var excludedInterfaceMethods = hubType.GetInterfaceMap(typeof(IDisposable)).TargetMethods;

        return methods.Except(excludedInterfaceMethods).Where(IsHubMethod);
    }

    private static bool IsHubMethod(MethodInfo methodInfo) {
        // Exclude methods inherited from object and special methods (like property getters/setters)
        var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType!;
        if (typeof(object) == baseDefinition || methodInfo.IsSpecialName) {
            return false;
        }

        // Exclude methods implemented on Hub
        var baseType = baseDefinition.IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
        return typeof(Hub) != baseType;
    }

    private static void AttachMethods(OpenApiDocument document, Type hubType, string path, DocumentProcessorContext context) {
        var friendlyHubName = MakeFriendlyHubName(hubType.Name);

        foreach (var (operationMethod, method) in GetHubMethods(hubType)) {
            // Add WebSocket endpoint documentation
            var pathItem = new OpenApiPathItem {
                Description = "WebSocket endpoint for event streaming"
            };

            var operation = new OpenApiOperation {
                Summary = method.GetXmlDocsSummary(),
                Description = method.GetXmlDocsRemarks(),
                OperationId = hubType.Name + "!" + method.Name,
                Tags = { friendlyHubName },     // This sets the group name
                Schemes = new List<OpenApiSchema> { OpenApiSchema.Ws, OpenApiSchema.Wss },
            };

            foreach (var parameter in method.GetParameters()) {
                // Skip cancellation tokens as they aren't exposed on the API interface
                if (parameter.ParameterType == typeof(CancellationToken)) {
                    continue;
                }

                operation.Parameters.Add(new OpenApiParameter {
                    Name = parameter.Name,
                    Kind = OpenApiParameterKind.Query,
                    Description = parameter.GetXmlDocs(),
                    IsRequired = !parameter.IsOptional,
                    Schema = JsonSchema.FromType(parameter.ParameterType),
                    Position = parameter.Position
                });
            }

            GenerateSuccessResponse(method, "Success", operation, context);

            pathItem.Add(operationMethod, operation);
            document.Paths.Add(path + "!" + method.Name, pathItem);
        }
    }

    public static bool HasNullableReturnType(MethodInfo method) {
        var nullableContextAttribute = method.GetCustomAttribute<NullableContextAttribute>();

        if (nullableContextAttribute is not null) {
            return nullableContextAttribute.Flag == 2;
        }

        return false;
    }

    private static void GenerateSuccessResponse(MethodInfo methodInfo, string successXmlDescription, OpenApiOperation operation, DocumentProcessorContext context) {
        var returnParameter = methodInfo.ReturnParameter;
        var returnType = returnParameter.ParameterType;
        if (returnType == typeof(Task)) {
            returnType = typeof(void);
        }

        returnType = GenericResultWrapperTypes.RemoveGenericWrapperTypes(
            returnType, t => t.Name, t => t.GenericTypeArguments[0]);

        if (IsVoidResponse(returnType)) {
            operation.Responses["200"] = new OpenApiResponse {
                Description = successXmlDescription
            };
        } else {
            var returnParameterAttributes = returnParameter?.GetCustomAttributes(false)?.OfType<Attribute>() ?? [];
            var contextualReturnParameter = returnType.ToContextualType(returnParameterAttributes);

            var isNullable = HasNullableReturnType(methodInfo);

            var settings = context.Settings.SchemaSettings;
            var typeDescription = settings.ReflectionService.GetDescription(contextualReturnParameter, context.Settings.SchemaSettings);
            var responseSchema = context.SchemaGenerator.GenerateWithReferenceAndNullability<NJsonSchema.JsonSchema>(
                contextualReturnParameter, isNullable, context.SchemaResolver);

            operation.Responses["200"] = new OpenApiResponse {
                Description = successXmlDescription,
                IsNullableRaw = isNullable,
                Schema = responseSchema,
            };
        }
    }

    private static bool IsVoidResponse(Type returnType)
        => returnType == null || returnType.FullName == "System.Void";

    public void Process(DocumentProcessorContext context) {
        // Get the paths of all registered Hubs
        foreach (var (hubType, path) in _hubEndpointProvider.GetHubEndpoints()) {
            AttachMethods(context.Document, hubType, path, context);
        }
    }
}
