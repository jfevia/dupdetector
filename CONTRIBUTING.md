# Contributing

## Getting started

```bash
dotnet build
dotnet test
```

## Before opening a pull request

```bash
dotnet format --verify-no-changes
./build/Test-Coverage.ps1
```

The coverage script fails below **100% line and 100% branch** coverage. This is a hard gate, not a
target: code that cannot be covered should be deleted or restructured rather than excluded.

## Conventions

- **Layering is enforced by project references.** `DupDetector.Core` references only Roslyn.
  `Sources` and `Reporting` reference `Core` and never each other. `Cli` is the only project allowed
  to touch `System.Console`.
- **Diagnostics are data.** Library code returns diagnostics; only `Cli` decides how to present them.
- **Files are UTF-8 with a byte-order mark.**
- **Comments explain what the code cannot.** One line, and only where intent is not self-evident.
- **Invalid states should be unrepresentable.** Prefer validating on assignment over checking later.

## Tests

Every behavioural change needs a test that would fail without it. When fixing a defect, name the test
after the behaviour that was wrong, so a regression is recognised rather than rediscovered — see
`tests/DupDetector.Cli.Tests/AuditRegressionTests.cs`.