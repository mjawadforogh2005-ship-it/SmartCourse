using System.Linq;
using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

public static class AcademicPlanningService
{
    // Check if a student has room for more credits
    public static bool HasRemainingCreditCapacity(RegistrationState state, string studentId, int additionalCredits)
    {
        var student = state.Students.FirstOrDefault(s => s.Id == studentId);
        if (student is null) return false;

        var currentCredits = RegistrationRules.CurrentCredits(state, studentId);
        return currentCredits + additionalCredits <= student.CreditLimit;
    }

    // Recommend courses they are eligible for but not enrolled in
    public static IEnumerable<Course> RecommendCourses(RegistrationState state, string studentId)
    {
        var student = state.Students.FirstOrDefault(s => s.Id == studentId);
        if (student is null) yield break;

        var enrolledCourseIds = state.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => state.Sections.First(s => s.Id == e.SectionId).CourseId)
            .ToHashSet();

        foreach (var course in state.Courses)
        {
            if (enrolledCourseIds.Contains(course.Id)) continue;
            if (!RegistrationRules.HasPrerequisites(student, course)) continue;
            
            // Don't recommend if it pushes them over credit limit
            if (!HasRemainingCreditCapacity(state, studentId, course.Credits)) continue;

            yield return course;
        }
    }
}