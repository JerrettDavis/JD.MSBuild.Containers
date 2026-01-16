# Contributing to JD.MSBuild.Containers

Thank you for your interest in contributing to JD.MSBuild.Containers! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Environment details:**
  - OS (Windows/Linux/macOS)
  - .NET SDK version (`dotnet --info`)
  - JD.MSBuild.Containers version
  - Docker version (`docker --version`)
- **Relevant logs** with `DockerLogVerbosity` set to `detailed`
- **Sample project** if possible (minimal reproduction)
- **Generated Dockerfile** if applicable

### Suggesting Features

Feature suggestions are welcome! Please:

- **Check existing feature requests** first
- **Describe the use case** clearly
- **Explain why** this feature would be useful
- **Provide examples** of how it would work

### Pull Requests

1. **Fork the repository** and create a branch from `main`
2. **Follow existing code style** and patterns
3. **Add tests** for new functionality
4. **Update documentation** as needed
5. **Ensure all tests pass** before submitting
6. **Write clear commit messages**

## Development Setup

### Prerequisites

- .NET SDK 8.0 or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- Docker Desktop or Docker Engine
- Git

### Building the Project

```bash
# Clone the repository
git clone https://github.com/jerrettdavis/JD.MSBuild.Containers.git
cd JD.MSBuild.Containers

# Restore dependencies
dotnet restore JD.MSBuild.Containers.sln

# Build the solution
dotnet build JD.MSBuild.Containers.sln
```

### Running Tests

```bash
# Run all tests
dotnet test JD.MSBuild.Containers.sln

# Run tests with coverage
dotnet test JD.MSBuild.Containers.sln --collect:"XPlat Code Coverage"
```

### Project Structure

- `src/JD.MSBuild.Containers/` - Main NuGet package with props/targets
- `src/JD.MSBuild.Containers.Tasks/` - MSBuild task implementations
- `tests/JD.MSBuild.Containers.Tests/` - Unit tests (TinyBDD/Xunit)

## Coding Guidelines

### General Principles

- Follow **Clean Code**, **DRY**, and **SOLID** principles
- Write **self-documenting code** with clear names
- Add **XML documentation** for all public APIs
- Keep methods **small and focused**
- Use **meaningful variable names**

### Testing Guidelines

- Use **TinyBDD** for BDD-style tests
- Follow **Given/When/Then** structure
- Test both **success and failure** scenarios
- Use **descriptive test names** with `[Scenario]` attribute
- Ensure tests are **isolated** and **repeatable**

### Documentation

- Update **README.md** for user-facing changes
- Add **XML comments** for all public APIs (docfx compatible)
- Include **examples** in documentation
- Update **IMPLEMENTATION_SUMMARY.md** for architectural changes

## Git Workflow

### Branch Naming

- `feature/description` - New features
- `fix/description` - Bug fixes
- `docs/description` - Documentation updates
- `refactor/description` - Code refactoring

### Commit Messages

Follow conventional commits format:

```
type(scope): short description

Longer explanation if needed.

Fixes #123
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

### Pull Request Process

1. **Update your fork** with the latest main branch
2. **Create a feature branch** from main
3. **Make your changes** with clear commits
4. **Run all tests** and ensure they pass
5. **Update documentation** as needed
6. **Push to your fork** and create a pull request
7. **Address review feedback** promptly

## Code Review

All submissions require review. We use GitHub pull requests for this purpose. The review process checks:

- **Code quality** and style consistency
- **Test coverage** for new functionality
- **Documentation** completeness
- **Build status** (CI must pass)
- **Security** (CodeQL analysis)

## Release Process

Releases are automated via GitHub Actions:

1. Merge to `main` triggers the release workflow
2. GitVersion calculates the version number
3. Build and test run on all target frameworks
4. NuGet packages are created and published
5. GitHub Release is created with release notes

## Questions?

If you have questions about contributing:

- Open an **issue** for discussion
- Check **existing issues** and pull requests
- Review the **documentation** in the repository

Thank you for contributing to JD.MSBuild.Containers!
