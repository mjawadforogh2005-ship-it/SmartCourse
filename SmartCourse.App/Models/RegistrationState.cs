using System.Collections.Immutable;

namespace SmartCourse.App.Models;

public sealed record Student(string Id, string Name, ImmutableArray<string> CompletedCourseIds, int CreditLimit);
public sealed record Course(string Id, string Title, int Credits, ImmutableArray<string> PrerequisiteCourseIds);
public sealed record MeetingSlot(DayOfWeek Day, TimeOnly Start, TimeOnly End);
public sealed record CourseSection(string Id, string CourseId, int Capacity, ImmutableArray<MeetingSlot> Schedule);
public sealed record Enrollment(string StudentId, string SectionId);
public sealed record RegistrationState(
    ImmutableArray<Student> Students,
    ImmutableArray<Course> Courses,
    ImmutableArray<CourseSection> Sections,
    ImmutableArray<Enrollment> Enrollments);
