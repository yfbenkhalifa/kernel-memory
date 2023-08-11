﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core;
using Microsoft.SemanticMemory.InteractiveSetup;

// Run `dotnet run setup` to run this code and setup the example
if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgService: false, cfgOrchestration: false);
}

/* Use MemoryServerlessClient to run the default import pipeline
 * in the same process, without distributed queues.
 *
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly.
 *
 * Note: no web service required, each file is processed in this process. */

var memory = new Memory(Builder.GetServiceProvider());

// =======================
// === UPLOAD ============
// =======================

// Uploading one file - This will create
// a new upload every time because no document ID is specified, and
// stored under the "default" user because no User ID is specified.
Console.WriteLine("Uploading file without document ID");
await memory.ImportDocumentAsync("file1-Wikipedia-Carbon.txt");

// Uploading only if the document has not been (successfully) uploaded already
if (!await memory.IsDocumentReadyAsync(userId: "user1", documentId: "doc001"))
{
    Console.WriteLine("Uploading doc001");
    await memory.ImportDocumentAsync("file1-Wikipedia-Carbon.txt",
        new DocumentDetails(userId: "user1", documentId: "doc001"));
}

// Uploading a document containing multiple files
Console.WriteLine("Uploading doc002");
await memory.ImportDocumentAsync(new Document(new[]
{
    "file2-Wikipedia-Moon.txt",
    "file3-lorem-ipsum.docx",
    "file4-SK-Readme.pdf"
}, new DocumentDetails("user1", "doc002")));

// Categorizing files with tags
if (!await memory.IsDocumentReadyAsync(userId: "user2", documentId: "doc003"))
{
    Console.WriteLine("Uploading doc003");
    await memory.ImportDocumentAsync("file5-NASA-news.pdf",
        new DocumentDetails("user2", "doc003")
            .AddTag("collection", "meetings")
            .AddTag("collection", "NASA")
            .AddTag("collection", "space")
            .AddTag("type", "news"));
}

// =======================
// === ASK ===============
// =======================

// Test with User 1 memory
var question = "What's Semantic Kernel?";
Console.WriteLine($"\n\nQuestion: {question}");

var answer = await memory.AskAsync("user1", question);
Console.WriteLine($"\nAnswer: {answer.Result}\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}

// Test with User 2 memory
question = "Any news from NASA about Orion?";
Console.WriteLine($"\n\nQuestion: {question}");

answer = await memory.AskAsync("user1", question);
Console.WriteLine($"\nUser 1 Answer: {answer.Result}\n");

answer = await memory.AskAsync("user2", question);
Console.WriteLine($"\nUser 2 Answer: {answer.Result}\n\n  Sources:\n");

foreach (var x in answer.RelevantSources)
{
    Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
}

// Test with tags
question = "What is Orion?";
Console.WriteLine($"\n\nQuestion: {question}");

var filter1 = new MemoryFilter().ByTag("type", "article");
var filter2 = new MemoryFilter().ByTag("type", "news");

answer = await memory.AskAsync("user2", question, filter1);
Console.WriteLine($"\nArticles: {answer.Result}\n\n");

answer = await memory.AskAsync("user2", question, filter2);
Console.WriteLine($"\nNews: {answer.Result}\n\n");
