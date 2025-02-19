﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GenerativeAI.Core;
using GenerativeAI.Microsoft;
using GenerativeAI.Microsoft.Extensions;
using GenerativeAI.Tests.Base;
using Microsoft.Extensions.AI;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace GenerativeAI.Tests.Microsoft;

/// <summary>
/// Tests for <see cref="GenerativeAIChatClient"/>.
/// Demonstrates a style similar to other Generative AI tests,
/// using xUnit and Shouldly for assertions.
/// </summary>
[TestCaseOrderer(
    ordererTypeName: "GenerativeAI.Tests.Base.PriorityOrderer",
    ordererAssemblyName: "GenerativeAI.Tests")]
public class GenerativeAIChatClient_Tests : TestBase
{
    private const string DefaultTestModelName = GoogleAIModels.DefaultGeminiModel;

    public GenerativeAIChatClient_Tests(ITestOutputHelper helper) : base(helper)
    {
    }

    #region Consoles

    /// <summary>
    /// Creates a minimal mock or fake platform adapter for testing.
    /// This can be replaced by a more feature-complete mock if needed.
    /// </summary>
    private IPlatformAdapter CreateTestPlatformAdapter()
    {
        // Return anything that implements IPlatformAdapter.
        // For illustration, we return a placeholder or a fake:
        return GetTestGooglePlatform();
    }

    

    #endregion

    #region Constructor Tests

    [Fact, TestPriority(1)]
    public void ShouldCreateWithBasicConstructor()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();

        // Act
        var client = new GenerativeAIChatClient(adapter);

        // Assert
        client.ShouldNotBeNull();
        client.model.ShouldNotBeNull();
        client.model.Model.ShouldBe(DefaultTestModelName);
        Console.WriteLine("GenerativeAIChatClient created successfully with the basic constructor.");
    }

    [Fact, TestPriority(2)]
    public void ShouldCreateWithCustomModelName()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var customModel = "my-custom-model";

        // Act
        var client = new GenerativeAIChatClient(adapter, customModel);

        // Assert
        client.ShouldNotBeNull();
        client.model.ShouldNotBeNull();
        client.model.Model.ShouldBe(customModel);
        Console.WriteLine($"GenerativeAIChatClient created with custom model: {customModel}");
    }

    #endregion

    #region CompleteAsync Tests

    [Fact, TestPriority(3)]
    public async Task ShouldThrowArgumentNullExceptionWhenChatMessagesIsNull()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var client = new GenerativeAIChatClient(adapter);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => { await client.CompleteAsync(null!); });
        Console.WriteLine("CompleteAsync threw ArgumentNullException as expected when chatMessages was null.");
    }

    [Fact, TestPriority(4)]
    public async Task ShouldReturnChatCompletionOnValidInput()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var client = new GenerativeAIChatClient(adapter);

        // We can simulate some ChatMessage list for testing:
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello! How are you?")
        };

        // We’ll stub out the model’s behavior by providing a minimal response
        // This would normally be mocked more extensively.
        // For demonstration, we assume GenerateContentAsync(...) works.

        // Act
        var result = await client.CompleteAsync(messages);

        // Assert
        result.ShouldNotBeNull();
        result.Choices.ShouldNotBeNull();
        Console.WriteLine(result.Choices[0].Text);
        
        
        Console.WriteLine("CompleteAsync returned a valid ChatCompletion when given valid input.");
    }

    #endregion

    #region CompleteStreamingAsync Tests

    [Fact, TestPriority(5)]
    public async Task ShouldThrowArgumentNullExceptionWhenChatMessagesIsNullForStreaming()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var client = new GenerativeAIChatClient(adapter);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in client.CompleteStreamingAsync(null!))
            {
                // Should never get here
                Console.WriteLine(_.Text ?? "null");
            }
        });
        Console.WriteLine("CompleteStreamingAsync threw ArgumentNullException as expected when chatMessages was null.");
    }

    [Fact, TestPriority(6)]
    public async Task ShouldReturnStreamOfMessagesOnValidInput()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var client = new GenerativeAIChatClient(adapter);

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Stream test")
        };

        // Act
        var updates = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in client.CompleteStreamingAsync(messages))
        {
            updates.Add(update);
            Console.WriteLine(update.Text ?? "null");
        }

        // Assert
        updates.ShouldNotBeEmpty();
        Console.WriteLine("CompleteStreamingAsync returned a stream of updates on valid input.");
    }

    #endregion

    #region GetService Tests

    [Fact, TestPriority(7)]
    public void ShouldReturnSelfFromGetServiceIfTypeMatches()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var client = new GenerativeAIChatClient(adapter);

        // Act
        var service = client.GetService(typeof(GenerativeAIChatClient));

        // Assert
        service.ShouldNotBeNull();
        service.ShouldBeOfType<GenerativeAIChatClient>();
        service.ShouldBe(client);
        Console.WriteLine("GetService returned the correct instance when serviceType matches the client type.");
    }

    [Fact, TestPriority(8)]
    public void ShouldReturnNullFromGetServiceIfTypeDoesNotMatch()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var client = new GenerativeAIChatClient(adapter);

        // Act
        var service = client.GetService(typeof(object));

        // Assert
        service.ShouldBeNull();
        Console.WriteLine("GetService returned null when the requested serviceType did not match.");
    }

    #endregion

    #region Metadata Tests

    [Fact, TestPriority(9)]
    public void MetadataShouldBeNullByDefault()
    {
        // Arrange
        var adapter = CreateTestPlatformAdapter();
        var client = new GenerativeAIChatClient(adapter);

        // Assert
        client.Metadata.ShouldBeNull();
        Console.WriteLine("By default, Metadata is null in GenerativeAIChatClient.");
    }

    #endregion

    
}