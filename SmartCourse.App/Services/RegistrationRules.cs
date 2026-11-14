using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

public static class RegistrationRules
{
    public static bool HasPrerequisites(Student student, Course course) =>
        course.PrerequisiteCourseIds.All(student.CompletedCourseIds.Contains);

    public static bool HasCapacity(RegistrationState state, CourseSection section) =>
        state.Enrollments.Count(e => e.SectionId == section.Id) < section.Capacity;

    // Half-open intervals: a class ending at 10:00 may precede one starting at 10:00.
    public static bool Overlaps(MeetingSlot left, MeetingSlot right) =>
        left.Day == right.Day && left.Start < right.End && right.Start < left.End;

    public static bool HasScheduleConflict(RegistrationState state, string studentId, CourseSection target) =>
        state.Enrollments.Where(e => e.StudentId == studentId)
            .Select(e => state.Sections.First(s => s.Id == e.SectionId))
            .Any(s => s.Schedule.Any(a => target.Schedule.Any(b => Overlaps(a, b))));

    public static int CurrentCredits(RegistrationState state, string studentId) =>
        state.Enrollments.Where(e => e.StudentId == studentId)
            .Select(e => state.Sections.First(s => s.Id == e.SectionId).CourseId)
            .Distinct().Sum(id => state.Courses.First(c => c.Id == id).Credits);
}
