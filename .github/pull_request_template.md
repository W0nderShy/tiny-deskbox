## Summary

Describe the user-visible result and why the change is needed.

## Test Evidence

- [ ] `dotnet test DeskBox.sln -c Release`
- [ ] `dotnet build DeskBox.sln -c Release --no-restore`
- [ ] Manual verification completed when Windows shell behavior changed

## Screenshots

Add before-and-after screenshots for UI changes. Remove private file names, documents, account details, and personal paths.

## Checklist

- [ ] The change is focused and contains no unrelated refactor.
- [ ] Tests were added or updated for behavior changes.
- [ ] File movement, deletion, startup, and recovery risks were considered.
- [ ] Existing `%LocalAppData%\DeskBox\` data remains compatible, or migration is documented.
- [ ] English and Simplified Chinese README sections were updated when public behavior changed.
