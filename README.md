# SmartCourse

IFP project: Mohammad and Afridi. Requires .NET 10.

Run: `dotnet run --project SmartCourse.App`

Test: `dotnet test SmartCourse.slnx`

Mohammad's shared immutable models, Result/Maybe, registration/drop rules, three-stage pipeline and tests are implemented. The console is an in-memory demo; changes reset on exit. S001 / B succeeds, S002 / B fails prerequisites.

## Afridi integration contract

Namespaces: SmartCourse.App.Models, SmartCourse.App.Functional, SmartCourse.App.Services. Shared records are in Models/RegistrationState.cs. Collections are ImmutableArray. Enrollment presence means active enrollment; no history/status field is used.

RegistrationService.Register(state, studentId, sectionId) and Drop(state, studentId, sectionId) return Result<RegistrationState>. Pattern match Success(Value) or Failure(Error). Replace application state only on success. Register accepts an optional pure Func<Student, Course, bool> policy.

The pipeline is ValidateStudent -> ValidateSection -> ValidateEligibility, composed with Bind. Map constructs the updated state. Core functions have no console/file I/O. IDs are case-sensitive in the core; the console normalizes input. Meetings use half-open intervals, allowing adjacent classes. Overnight meetings must be split into same-day slots.

Persistence must validate initialized collections, unique nonblank IDs, positive course credits, nonnegative capacity/credit limits, valid foreign keys, unique student/course enrollment, and meeting start < end. Core calculations assume structurally valid snapshots.

Afridi's planning, transfers, LINQ reports, extension methods, lazy processing, JSON/sample data and tests remain to be implemented. Program.cs must integrate their actual APIs when available. For transfers, verify original enrollment and same course, build a tentative dropped snapshot, then register into the target. On failure retain the original state.

## Defense

Explain immutability, Map versus Bind, missing lookups with Maybe, the policy function, and failure propagation. Demonstrate successful registration, missing prerequisites, dropping, and tests. Change a credit limit and predict the result.

This is not the complete team submission. Verify the original requirements PDF and acknowledge AI assistance as required by your instructor.
