using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

public static class CourseReports
{
    // Report 1: Group by Course, Aggregate (Count), OrderBy with Tie-Breaker
    public static IEnumerable<object> GetCourseEnrollmentSummary(RegistrationState state)
    {
        return state.Enrollments
            .Select(e => state.Sections.First(s => s.Id == e.SectionId)) // Projection
            .GroupBy(s => s.CourseId)                                     // Grouping
            .Select(g => new 
            {
                CourseId = g.Key,
                TotalEnrollments = g.Count(),                         // Aggregation
                Capacity = g.Sum(s => s.Capacity)                     // Aggregation
            })
            .OrderByDescending(x => x.TotalEnrollments)               // Ordering
            .ThenBy(x => x.CourseId);                                 // Tie-breaker
    }

    // Report 2: Filtering, Projection, and Aggregation (Sum)
    public static IEnumerable<(string StudentId, int TotalCredits)> GetStudentCreditLoads(RegistrationState state)
    {
        return state.Students
            .Where(s => s.CreditLimit > 0)                            // Filtering
            .Select(s => (
                s.Id, 
                TotalCredits: state.Enrollments
                    .Where(e => e.StudentId == s.Id)
                    .Select(e => state.Sections.First(sec => sec.Id == e.SectionId).CourseId)
                    .Distinct()
                    .Sum(courseId => state.Courses.First(c => c.Id == courseId).Credits) // Aggregation
            ));
    }
}