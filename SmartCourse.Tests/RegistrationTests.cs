using SmartCourse.App.Models;
using SmartCourse.App.Functional;
using SmartCourse.App.Services;
namespace SmartCourse.Tests;

public class RegistrationTests
{
    static MeetingSlot Slot(int a, int b) => new(DayOfWeek.Monday, new TimeOnly(a, 0), new TimeOnly(b, 0));
    static RegistrationState Sample() => new([new Student("S", "Mohammad", ["PRE"], 6)],
        [new Course("C", "Programming", 3, ["PRE"]), new Course("D", "Math", 3, [])],
        [new CourseSection("A", "C", 1, [Slot(9, 10)]), new CourseSection("B", "D", 2, [Slot(10, 11)]), new CourseSection("A2", "C", 2, [])], []);
    static RegistrationState Ok(Result<RegistrationState> r) => Assert.IsType<Result<RegistrationState>.Success>(r).Value;
    static void Error(Result<RegistrationState> r, string message) => Assert.Equal(message, Assert.IsType<Result<RegistrationState>.Failure>(r).Error);
    [Fact] public void RegisterPreservesOriginal() { var s = Sample(); Assert.Single(Ok(RegistrationService.Register(s, "S", "A")).Enrollments); Assert.Empty(s.Enrollments); }
    [Theory]
    [InlineData("missing", "A", "Student not found.")]
    [InlineData("S", "missing", "Section not found.")]
    public void MissingLookup(string s, string c, string error) => Error(RegistrationService.Register(Sample(), s, c), error);
    [Fact] public void PrerequisitesRequired() { var s = Sample(); s = s with { Students = [s.Students[0] with { CompletedCourseIds = [] }] }; Error(RegistrationService.Register(s, "S", "A"), "Missing prerequisites."); }
    [Fact] public void CapacityRequired() { var s = Sample(); s = s with { Sections = s.Sections.SetItem(0, s.Sections[0] with { Capacity = 0 }) }; Error(RegistrationService.Register(s, "S", "A"), "Section is full."); }
    [Fact] public void DuplicateCourseRejected() { var s = Ok(RegistrationService.Register(Sample(), "S", "A")); Error(RegistrationService.Register(s, "S", "A2"), "Already enrolled in this course."); }
    [Fact] public void AdjacentClassesAndExactCreditLimitAllowed() { var s = Ok(RegistrationService.Register(Sample(), "S", "A")); Assert.Equal(2, Ok(RegistrationService.Register(s, "S", "B")).Enrollments.Length); }
    [Fact] public void CreditOverflowRejected() { var s = Sample(); s = s with { Students = [s.Students[0] with { CreditLimit = 2 }] }; Error(RegistrationService.Register(s, "S", "A"), "Credit limit exceeded."); }
    [Fact] public void ConflictRejected() { var s = Ok(RegistrationService.Register(Sample(), "S", "A")); s = s with { Sections = s.Sections.SetItem(1, s.Sections[1] with { Schedule = [Slot(9, 11)] }) }; Error(RegistrationService.Register(s, "S", "B"), "Timetable conflict."); Assert.Single(s.Enrollments); }
    [Fact] public void DifferentDaysAllowed() => Assert.False(RegistrationRules.Overlaps(Slot(9, 10), Slot(9, 10) with { Day = DayOfWeek.Tuesday }));
    [Fact] public void PolicyApplied() => Error(RegistrationService.Register(Sample(), "S", "A", (_, _) => false), "Additional eligibility rule rejected registration.");
    [Fact] public void EarlyFailureSkipsPolicy() { var called = false; RegistrationService.Register(Sample(), "missing", "A", (_, _) => { called = true; return true; }); Assert.False(called); }
    [Fact] public void DropPreservesOriginalAndFreesSeat() { var s = Ok(RegistrationService.Register(Sample(), "S", "A")); var next = Ok(RegistrationService.Drop(s, "S", "A")); Assert.Single(s.Enrollments); Assert.Empty(next.Enrollments); Assert.Single(Ok(RegistrationService.Register(next, "S", "A")).Enrollments); }
    [Fact] public void MissingEnrollmentRejected() => Error(RegistrationService.Drop(Sample(), "S", "A"), "Enrollment not found.");
    [Fact] public void MissingStudentCannotDrop() => Error(RegistrationService.Drop(Sample(), "missing", "A"), "Student not found.");
    [Fact] public void FailureSkipsMapAndBind() { Result<int> r = new Result<int>.Failure("stop"); Assert.Equal("stop", Assert.IsType<Result<int>.Failure>(r.Map<int>(_ => throw new Exception()).Bind<int>(_ => throw new Exception())).Error); }
    [Fact] public void SuccessTransforms() { Result<int> r = new Result<int>.Success(2); Assert.Equal(6, Assert.IsType<Result<int>.Success>(r.Map(x => x + 1).Bind<int>(x => new Result<int>.Success(x * 2))).Value); }
    [Fact] public void MaybeHandlesMissingAndPresent() { Assert.IsType<Result<string>.Failure>(Maybe<string>.FromNullable(null).ToResult("missing")); Assert.Equal("found", Assert.IsType<Result<string>.Success>(Maybe<string>.FromNullable("found").ToResult("missing")).Value); }
}
