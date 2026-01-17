# Code Coverage Configuration

This document describes the comprehensive code coverage setup for **JD.MSBuild.Containers**.

## Overview

Code coverage is fully enabled and integrated into our CI/CD pipeline, providing:
- ✅ Automated coverage collection on every PR and release
- ✅ HTML reports uploaded as CI artifacts
- ✅ Coverage summaries posted as PR comments
- ✅ Integration with Codecov for historical tracking and badges
- ✅ Configurable coverage thresholds and filters

## Components

### 1. Test Project Configuration

**Location:** `tests/JD.MSBuild.Containers.Tests/JD.MSBuild.Containers.Tests.csproj`

The test project includes the `coverlet.collector` package:

```xml
<PackageReference Include="coverlet.collector" Version="6.0.4" />
```

This enables code coverage collection during test execution using the cross-platform Coverlet collector.

### 2. CI Workflow Integration

**Location:** `.github/workflows/ci.yml`

#### PR Checks Job

The `pr-checks` job runs on every pull request and includes:

**Test Execution with Coverage:**
```yaml
- name: Test with coverage
  run: |
    dotnet test JD.MSBuild.Containers.sln \
      --configuration Release \
      --no-build \
      --collect:"XPlat Code Coverage" \
      -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura \
      -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include="[JD.MSBuild.Containers*]*" \
      -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[*Tests]*"
```

**Coverage Report Generation:**
- Uses **ReportGenerator** to create comprehensive reports
- Generates multiple formats: HtmlInline, Cobertura, TextSummary, lcov, Badges
- Applies assembly and file filters to exclude test code

**Report Distribution:**
1. **HTML Report Artifact** - Uploaded for download and offline viewing
2. **PR Comment** - Coverage summary posted directly on the PR
3. **Codecov Upload** - Sent to Codecov for historical tracking and analysis

#### Release Job

The `release` job runs on pushes to `main` and includes:

**Coverage Upload:**
- Collects coverage during release builds
- Uploads to Codecov with `fail_ci_if_error: false` (non-blocking for releases)

### 3. Codecov Configuration

**Location:** `codecov.yml`

Configures which files to ignore in coverage calculations:

```yaml
ignore:
  - "**/*.Tests/**"
  - "**/*Tests*/**"
```

This ensures test projects don't inflate coverage metrics.

### 4. Coverage Filters

#### Include Filters
- `[JD.MSBuild.Containers*]*` - All types in JD.MSBuild.Containers namespaces

#### Exclude Filters
- `[*Tests]*` - All test assemblies
- `**/*.Tests/*` - Test project files
- `**/*Tests*/**` - Any test-related paths

## Viewing Coverage Reports

### Local Development

Run tests with coverage locally:

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Find the coverage file
find . -name "coverage.cobertura.xml"

# Generate HTML report
reportgenerator \
  -reports:"**/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html"

# Open the report
open coverage-report/index.html  # macOS
xdg-open coverage-report/index.html  # Linux
start coverage-report/index.html  # Windows
```

### CI Artifacts

1. Navigate to the GitHub Actions run
2. Scroll to **Artifacts** section
3. Download `coverage-report` artifact
4. Extract and open `index.html`

### PR Comments

Coverage summaries are automatically posted to PRs:

```
## Code Coverage
Summary:
  Line coverage: 85.2%
  Branch coverage: 78.4%
  ...
```

### Codecov Dashboard

Visit: https://codecov.io/gh/JerrettDavis/JD.MSBuild.Containers

Features:
- Historical coverage trends
- Per-commit coverage changes
- File-level coverage visualization
- Coverage badges for README

## Coverage Badges

Add Codecov badge to README:

```markdown
[![codecov](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Containers/branch/main/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Containers)
```

## Coverage Thresholds

### Current Configuration

No hard thresholds are enforced (tests don't fail on low coverage).

### Adding Thresholds

To enforce minimum coverage thresholds, update `codecov.yml`:

```yaml
coverage:
  status:
    project:
      default:
        target: 80%
        threshold: 5%
    patch:
      default:
        target: 80%
```

Or add to the workflow:

```yaml
- name: Check coverage threshold
  run: |
    COVERAGE=$(grep -oP 'Line coverage: \K[\d.]+' coverage-report/Summary.txt)
    if (( $(echo "$COVERAGE < 80" | bc -l) )); then
      echo "Coverage $COVERAGE% is below threshold of 80%"
      exit 1
    fi
```

## Best Practices

### Writing Testable Code

1. **Keep code loosely coupled** - Easier to test in isolation
2. **Use dependency injection** - Mock external dependencies
3. **Separate concerns** - Business logic vs infrastructure
4. **Make members virtual** - Enable mocking when needed

### Improving Coverage

1. **Focus on critical paths** - Test business logic thoroughly
2. **Cover edge cases** - Null values, empty collections, boundary conditions
3. **Test error handling** - Exception paths and validation
4. **Integration tests** - Complement unit tests with end-to-end scenarios

### Excluding Code from Coverage

For generated code or code that can't be tested:

```csharp
[ExcludeFromCodeCoverage]
public class GeneratedClass
{
    // ...
}
```

## Troubleshooting

### Coverage Not Collected

**Issue:** No coverage reports generated

**Solution:**
1. Ensure `coverlet.collector` package is referenced
2. Verify `--collect:"XPlat Code Coverage"` argument
3. Check that tests are actually running
4. Look for `TestResults/*/coverage.cobertura.xml` files

### Incorrect Coverage Metrics

**Issue:** Coverage includes test code or other excluded files

**Solution:**
1. Verify include/exclude filters in CI workflow
2. Check `codecov.yml` ignore patterns
3. Update ReportGenerator assembly and file filters

### Codecov Upload Fails

**Issue:** `CODECOV_TOKEN` error

**Solution:**
1. Ensure `CODECOV_TOKEN` secret is set in GitHub repository settings
2. Verify token has correct permissions
3. Check Codecov account is properly linked to repository

## References

- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [Codecov Documentation](https://docs.codecov.com/)
- [.NET Test Coverage](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)

## Summary

✅ **Code coverage is fully configured and operational**

- Tests collect coverage automatically in CI
- Multiple report formats generated (HTML, Cobertura, lcov)
- Coverage summaries posted to PRs
- Historical tracking via Codecov
- Proper filters exclude test code
- Reports available as CI artifacts

No additional setup required - the system is production-ready.
