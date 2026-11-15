using SmartCourse.App.Models;
using SmartCourse.App.Services;

namespace SmartCourse.App.Services;

public static class CourseReports
{
    // Report 1: Group by Course, Aggregate (Count), OrderBy with Tie-Breaker
    // BUG FIX 1: Count distinct sections to avoid double-counting capacity
    public static IEnumerable<object> GetCourseEnrollmentSummary(RegistrationState state)
    {
        return state.Enrollments
            .Select(e => state.Sections.First(s => s.Id == e.SectionId))
            .GroupBy(s => s.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                TotalEnrollments = g.Count(), // Total enrolled students across all sections
                Capacity = g.Select(s => new { s.Id, s.Capacity }).Distinct().Sum(x => x.Capacity) // Sum distinct section capacities
            })
            .OrderByDescending(x => x.TotalEnrollments)
            .ThenBy(x => x.CourseId);
    }

    // Report 2: Filtering, Projection, and Aggregation (Sum)
    // BUG FIX 5: Uses the custom extension method GetEnrollmentsForStudent
    public static IEnumerable<(string StudentId, int TotalCredits)> GetStudentCreditLoads(RegistrationState state)
    {
        return state.Students
            .Where(s => s.CreditLimit > 0)
            .Select(s => (
                s.Id,
                TotalCredits: state.GetEnrollmentsForStudent(s.Id)
                    .Select(e => state.Sections.First(sec => sec.Id == e.SectionId).CourseId)
                    .Distinct()
                    .Sum(courseId => state.Courses.First(c => c.Id == courseId).Credits)
            ));
    }
}