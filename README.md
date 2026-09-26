# HTML Crawler

A console application in **C# (.NET 8)** that parses an HTML document into a tree and lets you query, edit, copy and compress it with simple commands.

Built as a 2nd-year course project at the **Technical University of Sofia**. The assignment did not allow built-in collections or library helpers, so all data structures and string utilities are implemented from scratch.

## Features

- **Custom HTML parser**: reads an HTML file, builds a DOM-like tree and validates the structure.
- **XPath-style search**: find elements by path, with wildcards, positions and attributes.
- **Editing**: replace the content of matched elements with text or an HTML fragment, or copy one part of the document into another.
- **Multi-threaded search**: runs a search across several worker threads using a custom thread-safe work queue (`lock` / `Monitor`).
- **Huffman compression**: saves the document to a compressed archive and loads it back.
- **Custom data structures**: stack, queue, priority queue, result list and a hash table for element attributes.

## Getting started

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download)

```bash
cd HTMLCrawler
dotnet run -- "testdata/example.html"
```

## Commands

| Command | Description |
|---|---|
| `PRINT "//path"` | Print all elements that match the path |
| `PRINTP "//path" [threads]` | Same search, run in parallel (default: 4 threads) |
| `SET "//path" "text or <html>"` | Replace the content of matched elements |
| `COPY "//source" "//target"` | Copy matched elements into the target |
| `SAVE "archive.huff"` | Save the document with Huffman compression |
| `LOAD "archive.huff"` | Load a compressed document |
| `EXIT` | Quit |

### Path examples

```text
PRINT "//"                           the whole document
PRINT "//html/body/p"                all <p> elements in <body>
PRINT "//html/body/p[1]"             the first <p>
PRINT "//html/body/table[@id='table2']/tr/td"
PRINT "//html/body/*"                all children of <body>
```

## Project structure

| File | Responsibility |
|---|---|
| `HtmlParser.cs` | Turns the HTML text into a tree of `HtmlNode` |
| `HtmlNode.cs` | Tree node: tag, attributes, children, validation, printing |
| `HtmlSearcher.cs` | Path parsing and (parallel) search |
| `CommandExecutor.cs` | Reads and runs user commands |
| `HuffmanCompressor.cs`, `HuffmanNode.cs` | Compression and decompression |
| `WorkQueue.cs` | Thread-safe queue for the worker threads |
| `MyStack.cs`, `MyPriorityQueue.cs`, `ResultList.cs`, `AttributeTable.cs`, `CharBuf.cs` | Custom data structures |
