﻿using System.Net;
using GenerativeAI.Tests.Base;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace GenerativeAI.Tests.Clients;

[TestCaseOrderer(
    ordererTypeName: "GenerativeAI.Tests.Base.PriorityOrderer",
    ordererAssemblyName: "GenerativeAI.Tests")]
public class DocumentsClient_Tests : TestBase
{
    public DocumentsClient_Tests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact, TestPriority(1)]
    public async Task ShouldCreateDocumentAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        var parent = "corpora/test-corpus-id";
        var newDocument = new Document
        {
            DisplayName = "Test Document",
            CustomMetadata = new List<CustomMetadata>
            {
                new CustomMetadata { Key = "author", StringValue = "John Doe" }
            }
        };

        // Act
        var result = await client.CreateDocumentAsync(parent, newDocument);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldNotBeNullOrEmpty();
        result.DisplayName.ShouldBe("Test Document");

        Console.WriteLine($"Document Created: Name={result.Name}, DisplayName={result.DisplayName}");
    }

    [Fact, TestPriority(2)]
    public async Task ShouldGetDocumentAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        var parent = "corpora/test-corpus-id";
        var documentList = await client.ListDocumentsAsync(parent);
        var testDocument = documentList.Documents.FirstOrDefault();
        var documentName = testDocument.Name;

        // Act
        var result = await client.GetDocumentAsync(documentName);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(documentName);
        result.DisplayName.ShouldNotBeNullOrEmpty();

        Console.WriteLine($"Retrieved Document: Name={result.Name}, DisplayName={result.DisplayName}");
    }

    [Fact, TestPriority(3)]
    public async Task ShouldListDocumentsAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        const int pageSize = 10;
        var parent = "corpora/test-corpus-id";

        // Act
        var result = await client.ListDocumentsAsync(parent, pageSize);

        // Assert
        result.ShouldNotBeNull();
        result.Documents.ShouldNotBeNull();
        result.Documents.Count.ShouldBeGreaterThan(0);
        result.Documents.Count.ShouldBeLessThanOrEqualTo(pageSize);

        foreach (var document in result.Documents)
        {
            document.Name.ShouldNotBeNullOrEmpty();
            document.DisplayName.ShouldNotBeNullOrEmpty();
        }

        Console.WriteLine($"Listed {result.Documents.Count} Documents");
    }

    [Fact, TestPriority(4)]
    public async Task ShouldUpdateDocumentAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        var parent = "corpora/test-corpus-id";
        var documentList = await client.ListDocumentsAsync(parent);
        var testDocument = documentList.Documents.FirstOrDefault();
        var updatedDocument = new Document
        {
            DisplayName = "Updated Document Name"
        };
        const string updateMask = "displayName";

        // Act
        var result = await client.UpdateDocumentAsync(testDocument.Name, updatedDocument, updateMask);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(testDocument.Name);
        result.DisplayName.ShouldBe("Updated Document Name");

        Console.WriteLine($"Updated Document: Name={result.Name}, DisplayName={result.DisplayName}");
    }

    [Fact, TestPriority(5)]
    public async Task ShouldDeleteDocumentAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        var parent = "corpora/test-corpus-id";
        var documentList = await client.ListDocumentsAsync(parent);
        var testDocument = documentList.Documents.LastOrDefault();

        // Act and Assert
        await Should.NotThrowAsync(async () => await client.DeleteDocumentAsync(testDocument.Name));
        Console.WriteLine($"Deleted Document: Name={testDocument.Name}");
    }

    [Fact, TestPriority(6)]
    public async Task ShouldQueryDocumentAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        var parent = "corpora/test-corpus-id";
        var documentList = await client.ListDocumentsAsync(parent);
        var testDocument = documentList.Documents.FirstOrDefault();
        var queryRequest = new QueryDocumentRequest
        {
            Query = "Test Query"
        };

        // Act
        var result = await client.QueryDocumentAsync(testDocument.Name, queryRequest);

        // Assert
        result.ShouldNotBeNull();
        result.RelevantChunks.ShouldNotBeNull();
        result.RelevantChunks.Count.ShouldBeGreaterThan(0);

        Console.WriteLine($"Queried Document: Name={testDocument.Name}, Retrieved {result.RelevantChunks.Count} Relevant Chunks");
    }

    [Fact, TestPriority(7)]
    public async Task ShouldHandleInvalidDocumentForRetrieveAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        const string invalidName = "corpora/test-corpus-id/documents/invalid-id";

        // Act
        var exception = await Should.ThrowAsync<Exception>(async () => await client.GetDocumentAsync(invalidName));

        // Assert
        exception.Message.ShouldNotBeNullOrEmpty();
        Console.WriteLine($"Handled Exception While Retrieving Document: {exception.Message}");
    }

    [Fact, TestPriority(8)]
    public async Task ShouldHandleInvalidDocumentForDeleteAsync()
    {
        // Arrange
        var client = new DocumentsClient(GetTestGooglePlatform());
        const string invalidName = "corpora/test-corpus-id/documents/invalid-id";

        // Act
        var exception = await Should.ThrowAsync<Exception>(async () => await client.DeleteDocumentAsync(invalidName));

        // Assert
        exception.Message.ShouldNotBeNullOrEmpty();
        Console.WriteLine($"Handled Exception While Deleting Document: {exception.Message}");
    }
}