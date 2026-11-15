using System.Linq;
using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

public static class AcademicPlanningService
{
    // BUG FIX 3: Reject negative additional credits
    public static bool HasRemainingCreditCapacity(RegistrationState state, string studentId, int additionalCredits)
    {
        if (additionalCredits < 0) return false; // Invalid request
        
        var student = state.Students.FirstOrDefault(s => s.Id == studentId);
        if (student is null) return false;

        var currentCredits = RegistrationRules.CurrentCredits(state, studentId);
        return currentCredits + additionalCredits <= student.CreditLimit;
    }

    // BUG FIX 5: Uses the custom extension method GetSectionsForCourse
    public static IEnumerable<Course> RecommendCourses(RegistrationState state, string studentId)
    {
        var student = state.Students.FirstOrDefault(s => s.Id == studentId);
        if (student is null) yield break;

        // Use extension method to get enrolled course IDs
        var enrolledCourseIds = state.GetEnrollmentsForStudent(studentId)
            .Select(e => state.Sections.First(s => s.Id == e.SectionId).CourseId)
            .ToHashSet();

        foreach (var course in state.Courses)
        {
            if (enrolledCourseIds.Contains(course.Id)) continue;
            if (!RegistrationRules.HasPrerequisites(student, course)) continue;
            if (!HasRemainingCreditCapacity(state, studentId, course.Credits)) continue;

            // Only recommend if the course actually has sections available
            if (state.GetSectionsForCourse(course.Id).Any()) 
            {
                yield return course;
            }
        }
    }
}