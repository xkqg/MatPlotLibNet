# Contributing to MatPlotLibNet

Thank you for your interest in contributing to MatPlotLibNet! This guide will help you get started.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/<your-username>/MatPlotLibNet.git`
3. Create a branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Push and open a pull request

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An IDE such as Visual Studio 2022, VS Code, or JetBrains Rider

## Building

```bash
dotnet build
```

## Running Tests

```bash
dotnet test
```

## Project Structure

```
Src/          Source projects (one folder per package)
Tst/          Test projects (mirrors Src/ layout)
Samples/      Runnable sample projects
Benchmarks/   BenchmarkDotNet performance suite
```

## Coding Conventions

- Use C# records for immutable data types
- Follow the existing fluent API patterns
- No `Base` suffix on abstract classes — use descriptive names
- Prefer generic base classes with overrides over static methods when behavior is shared
- Keep the public API intuitive and discoverable

## Pull Request Guidelines

- Keep PRs focused on a single change
- Include tests for new functionality
- Ensure all existing tests pass
- Update documentation if your change affects the public API
- Follow the existing code style

## Reporting Issues

- Use GitHub Issues for bug reports and feature requests
- Include steps to reproduce for bugs
- Include the .NET version and OS you are using

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
