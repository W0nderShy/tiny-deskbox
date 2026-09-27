# Tiny DeskBox Open-Source Release Design

## Goal

Publish the existing Windows application as a searchable, welcoming public GitHub project named **Tiny DeskBox** in the repository `tiny-deskbox`.

## Positioning

Tiny DeskBox is a tiny, easy-to-use Windows desktop organizer. It groups desktop files and shortcuts into movable, resizable boxes while keeping data local. The public messaging emphasizes three advantages: small footprint, convenient desktop organization, and a low learning curve.

The GitHub repository description is:

> A tiny, easy-to-use Windows desktop organizer with movable boxes.

Repository topics are `windows`, `desktop-organizer`, `dotnet`, `wpf`, `productivity`, and `lightweight`.

## README Structure

The README is English-first for global search visibility, followed by a complete Simplified Chinese version. Both languages contain full prose rather than abbreviated summaries.

Each language includes:

- Product purpose and core value
- Real application screenshot
- Key features and advantages
- How boxes store and restore files
- Build and run instructions
- Single-file publishing instructions
- Data locations and recovery behavior
- Safety notes
- Contribution entry point
- Current limitations and roadmap

The top of the README includes Windows, .NET 8, MIT, and contributions-welcome badges. A language navigation line links directly to the English and Chinese sections.

## Visual Asset

The user-provided real desktop screenshot is copied to `docs/images/deskbox-preview.png` and embedded near the top of both language sections. The image is not altered to imply functionality that the application does not have.

## Contribution Files

- `CONTRIBUTING.md` explains environment setup, issue reporting, branch naming, tests, pull requests, and coding expectations.
- `.github/ISSUE_TEMPLATE/bug_report.yml` collects reproduction steps, expected behavior, Windows version, and application version.
- `.github/ISSUE_TEMPLATE/feature_request.yml` collects the problem, proposed workflow, and alternatives.
- `.github/pull_request_template.md` requests a summary, test evidence, screenshots for UI changes, and a checklist.
- `LICENSE` contains the MIT License with the current year and repository owner's GitHub identity.

## Repository Hygiene

The public repository includes source code, tests, build scripts, solution files, formal documentation, and the screenshot. It excludes binaries, build output, local logs, temporary visual-companion state, user-specific files, and local runtime data.

Existing diagnostic files such as `build.log`, `dotnet-check.log`, `winget.log`, and `shell-test.txt` remain on the computer but are ignored and not committed.

## GitHub Publication

Initialize the local project on branch `main`, create a coherent initial commit, create a public GitHub repository named `tiny-deskbox`, push `main`, configure the description and topics, and enable normal GitHub contribution workflows through Issues and Pull Requests.

GitHub CLI is not currently installed. Publication may use the authenticated GitHub browser session for repository creation and standard Git credentials for push. If authentication is unavailable, stop at the authentication boundary and ask the user to sign in; do not request or expose a personal access token.

## Verification

Before publication:

- Render-check all local README image paths.
- Verify English and Chinese sections contain the same substantive topics.
- Run the full Release test suite and build.
- Inspect the staged file list to ensure no logs, binaries, runtime data, or visual-companion files are included.
- After pushing, verify the repository page, default branch, README image, description, topics, license, and contribution files are visible.
