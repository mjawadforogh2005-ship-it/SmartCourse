using SmartCourse.App.Functional;
using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

// Inputs must be structurally valid snapshots (unique IDs, valid references and schedules).
// JSON structural validation belongs to the persistence layer.
public static class RegistrationService
{
    private sealed record Request(RegistrationState State, Student Student, CourseSection Section, Course Course);

    public static Result<RegistrationState> Register(RegistrationState state, string studentId, string sectionId,
        Func<Student, Course, bool>? additionalEligibility = null) =>
        ValidateStudent(state, studentId)
            .Bind(student => ValidateSection(state, student, sectionId))
            .Bind(request => ValidateEligibility(request, additionalEligibility ?? ((_, _) => true)))
            .Map(request => request.State with
            {
                Enrollments = request.State.Enrollments.Add(new Enrollment(studentId, sectionId))
            });

    private static Result<Student> ValidateStudent(RegistrationState state, string id) =>
        Maybe<Student>.FromNullable(state.Students.FirstOrDefault(s => s.Id == id))
            .ToResult("Student not found.");

    private static Result<Request> ValidateSection(RegistrationState state, Student student, string sectionId) =>
        Maybe<CourseSection>.FromNullable(state.Sections.FirstOrDefault(s => s.Id == sectionId))
            .ToResult("Section not found.")
            .Bind(section => Maybe<Course>.FromNullable(state.Courses.FirstOrDefault(c => c.Id == section.CourseId))
                .ToResult("Section refers to a missing course.")
                .Map(course => new Request(state, student, section, course)))
            .Bind<Request>(request => RegistrationRules.HasCapacity(state, request.Section)
                ? new Result<Request>.Success(request)
                : new Result<Request>.Failure("Section is full."));

    private static Result<Request> ValidateEligibility(Request request, Func<Student, Course, bool> policy)
    {
        var (state, student, section, course) = request;
        var alreadyEnrolled = state.Enrollments.Where(e => e.StudentId == student.Id)
            .Any(e => state.Sections.First(s => s.Id == e.SectionId).CourseId == course.Id);
        string? error = true switch
        {
            _ when alreadyEnrolled => "Already enrolled in this course.",
            _ when !RegistrationRules.HasPrerequisites(student, course) => "Missing prerequisites.",
            _ when RegistrationRules.HasScheduleConflict(state, student.Id, section) => "Timetable conflict.",
            _ when (long)RegistrationRules.CurrentCredits(state, student.Id) + course.Credits > student.CreditLimit => "Credit limit exceeded.",
            _ when !policy(student, course) => "Additional eligibility rule rejected registration.",
            _ => null
        };
        return error is null ? new Result<Request>.Success(request) : new Result<Request>.Failure(error);
    }

    public static Result<RegistrationState> Drop(RegistrationState state, string studentId, string sectionId) =>
        ValidateStudent(state, studentId).Bind<RegistrationState>(_ =>
        {
            var enrollment = state.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.SectionId == sectionId);
            return enrollment is null
                ? new Result<RegistrationState>.Failure("Enrollment not found.")
                : new Result<RegistrationState>.Success(state with { Enrollments = state.Enrollments.Remove(enrollment) });
        });
}
