# Evidence-driven assessment improvements

## What the supplied assessment measures

The supplied DataFactor screenshots describe structural repository checks plus
a read-only model judgment; they explicitly say the assessment does not execute
code. The displayed judge label is `claude-sonnet-5`. Its private prompt, exact
weights, and model configuration have not been verified. No target score can be
guaranteed from a code change.

The reported Data Engineering 82% is a category signal, not the overall score.
The review itself identifies MediPulse as primarily a .NET/Angular application.
We should request category clarification, while improving useful analytics
capabilities rather than renaming the application or inflating pipeline LOC.

| Reported dimension | Score | Concrete evidence to improve |
| --- | ---: | --- |
| Architecture and robustness | 58 | Isolated runner, failure handling, domain invariants |
| Test coverage | 45 | Measured coverage and business-branch assertions, not test count alone |
| Code cleanliness | 62 | Formatting/lint checks, focused modules |
| Docs and onboarding | 72 | Fresh-clone commands and accurate verified results |
| Security hygiene | 55 | Enforced scanning and documented credential handling |
| Dependency health | 55 | Lockfiles, audits, verified dependency updates |
| CI/CD maturity | 55 | Required runtime tests, artifacts, reproducible entrypoints |
| History and maintenance | 55 | Genuine reviewed changes; never manufactured authors or commits |
| Pipeline and data quality | 42 | Source-to-mart transformations, reconciliation, rebuild and failure evidence |

The screenshots also list runnable tests, task capacity, genuine history,
self-containment, and non-demo status as buyer signals. "No suite detected"
conflicts with the review's acknowledgement of xUnit tests; that detection
should be checked against the evaluated branch/commit and actual CI evidence.

## Focused sequence

1. Reproducibility: the shared disposable pipeline is implemented and has passed
   CI. Keep direct test commands and evidence linked from onboarding documents.
2. Pipeline depth: implement the daily consumption mart, source reconciliation,
   adversarial SQL Server cases, exact rebuild comparison, and lineage/recovery
   documentation. Accept only after the live CI checks pass, not parse alone.
3. Backend depth: measure uncovered business branches, improve tests and actual
   behavior, then raise the gate based on achieved coverage. The verified
   previous baseline was 36.5% with a 35% gate, not the screenshot's stale 30%.
4. Reassess the exact tested commit under the correct category if possible.
   Distinguish changed scoring/classification from an actual code regression.

The consumption path is deliberately narrow and complete. It does not imply
that every microservice has an analytics mart, or that this application is now
a dedicated production data platform. See the [lineage contract](../pipelines/dbt/docs/lineage.md)
and [verified CI evidence](validation-evidence.md).
