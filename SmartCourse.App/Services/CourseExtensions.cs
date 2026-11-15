using SmartCourse.App.Models;

namespace SmartCourse.App.Services; // Changed from .Extensions to .Services

public static class CourseExtensions
{
    // Extension 1: Get all sections for a specific course
    public static IEnumerable<CourseSection> GetSectionsForCourse(this RegistrationState state, string courseId) =>
        state.Sections.Where(s => s.CourseId == courseId);

    // Extension 2: Get active enrollments for a specific student
    public static IEnumerable<Enrollment> GetEnrollmentsForStudent(this RegistrationState state, string studentId) =>
        state.Enrollments.Where(e => e.StudentId == studentId);
}