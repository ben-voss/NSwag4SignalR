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

using System;
using Shouldly;

namespace NSwag4SignalR.Tests;

public sealed class GenericResultWrapperTypesTests {
    private sealed class MockType {
        public string Name { get; set; }
        public MockType? InnerType { get; set; }

        public MockType(string name, MockType? innerType = null)
        {
            Name = name;
            InnerType = innerType;
        }
    }

    [Theory]
    [InlineData("Task`1", "string")]
    [InlineData("ValueTask`1", "int")]
    [InlineData("JsonResult`1", "TestObject")]
    [InlineData("ActionResult`1", "TestObject")]
    public void RemoveGenericWrapperTypesRemovesTaskWrapper(string type, string innerType) {
        var inner = new MockType(innerType);
        var task = new MockType(type, inner);

        var result = GenericResultWrapperTypes.RemoveGenericWrapperTypes(
            task,
            t => t.Name,
            t => t.InnerType!
        );

        result.Name.ShouldBe(innerType);
    }

    [Fact]
    public void RemoveGenericWrapperTypesRemovesNestedWrappers() {
        var inner = new MockType("string");
        var actionResult = new MockType("ActionResult`1", inner);
        var task = new MockType("Task`1", actionResult);

        var result = GenericResultWrapperTypes.RemoveGenericWrapperTypes(
            task,
            t => t.Name,
            t => t.InnerType!
        );

        result.Name.ShouldBe("string");
    }

    [Fact]
    public void RemoveGenericWrapperTypesLeavesNonWrapperTypesUnchanged() {
        var type = new MockType("string");

        var result = GenericResultWrapperTypes.RemoveGenericWrapperTypes(
            type,
            t => t.Name,
            t => t.InnerType!
        );

        result.Name.ShouldBe("string");
    }

    [Fact]
    public void RemoveGenericWrapperTypesHandlesMultipleNestedWrappers() {
        var inner = new MockType("User");
        var actionResult = new MockType("ActionResult`1", inner);
        var valueTask = new MockType("ValueTask`1", actionResult);
        var task = new MockType("Task`1", valueTask);

        var result = GenericResultWrapperTypes.RemoveGenericWrapperTypes(
            task,
            t => t.Name,
            t => t.InnerType!
        );

        result.Name.ShouldBe("User");
    }

    [Fact]
    public void RemoveGenericWrapperTypesStopsAtFirstNonWrapper() {
        var inner = new MockType("MyCustomType");
        var task = new MockType("Task`1", inner);

        var result = GenericResultWrapperTypes.RemoveGenericWrapperTypes(
            task,
            t => t.Name,
            t => t.InnerType!
        );

        result.Name.ShouldBe("MyCustomType");
    }
}