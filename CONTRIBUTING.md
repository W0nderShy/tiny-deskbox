# Contributing to Tiny DeskBox

Thank you for helping make Tiny DeskBox better. Bug reports, focused feature proposals, documentation improvements, tests, and code changes are welcome.

## Before You Start

- Search existing issues before opening a new one.
- Keep proposals aligned with the project's small, local, easy-to-use character.
- Do not include private file names, personal paths, or documents from your desktop in logs or screenshots.
- For larger behavior or UI changes, open an issue first so the approach can be discussed before implementation.

## Development Setup

Requirements:

- Windows
- .NET 8 SDK
- Git

```powershell
git clone https://github.com/W0nderShy/tiny-deskbox.git
cd tiny-deskbox
dotnet restore DeskBox.sln
dotnet test DeskBox.sln -c Release
dotnet build DeskBox.sln -c Release --no-restore
```

Run the application from source:

```powershell
dotnet run --project src\DeskBox.App\DeskBox.App.csproj -c Release
```

## Reporting Bugs

Use the bug report form and provide:

- Reproduction steps that begin from a known state
- Expected and actual behavior
- Windows version and display configuration
- Tiny DeskBox commit or release version
- Relevant logs or screenshots with private information removed

File movement and deletion issues are especially important. Clearly state where the item began, which action was used, and where the item ended up.

## Proposing Features

Explain the user problem before describing a solution. Tiny DeskBox intentionally stays focused, so additions should make desktop organization safer or easier without turning the app into a general file manager or cloud platform.

## Branches and Commits

Use short branch names such as `fix/layout-recovery` or `feat/layout-export`. Write commits in the imperative mood and keep unrelated changes in separate commits.

## Pull Requests

- Keep the change focused and explain the user-visible result.
- Add or update tests when behavior changes.
- Run the Release tests and build before submitting.
- Include before-and-after screenshots for UI changes.
- Mention file movement, deletion, startup, or recovery risks explicitly.
- Update both English and Simplified Chinese README sections when public behavior changes.

## Code Style

- Follow the existing C# and WPF patterns in the repository.
- Prefer small, purpose-specific changes over broad refactors.
- Use structured serialization and Windows shell APIs instead of parsing paths or shell output manually.
- Preserve backward compatibility for existing `%LocalAppData%\DeskBox\` data whenever practical.

By contributing, you agree that your contribution will be licensed under the repository's MIT License.
