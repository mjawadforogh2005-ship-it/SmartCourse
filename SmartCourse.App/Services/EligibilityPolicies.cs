using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

public static class EligibilityPolicies
{
    // A curried function: Takes a minimum credit limit, returns a function that checks a student.
    public static Func<Student, bool> MakeCreditLimitPolicy(int minimumLimit)
    {
        return student => student.CreditLimit >= minimumLimit;
    }

    // Another configured function using partial application
    public static Func<Student, Course, bool> MakeStrictPrerequisitePolicy()
    {
        return (student, course) => 
            RegistrationRules.HasPrerequisites(student, course) && 
            student.CompletedCourseIds.Length > 0;
    }
}