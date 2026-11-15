using SmartCourse.App.Functional;
using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

public static class SectionTransferService
{
    public static Result<RegistrationState> Transfer(RegistrationState state, string studentId, string fromSectionId, string toSectionId)
    {
        // 1. Verify original enrollment exists
        var enrollment = state.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.SectionId == fromSectionId);
        if (enrollment is null) return new Result<RegistrationState>.Failure("Original enrollment not found.");

        // 2. Verify same course
        var fromSection = state.Sections.FirstOrDefault(s => s.Id == fromSectionId);
        var toSection = state.Sections.FirstOrDefault(s => s.Id == toSectionId);
        
        if (fromSection is null || toSection is null) 
            return new Result<RegistrationState>.Failure("One or both sections not found.");
            
        if (fromSection.CourseId != toSection.CourseId) 
            return new Result<RegistrationState>.Failure("Cannot transfer to a different course.");

        // 3. Build tentative dropped snapshot
        var tentativeState = state with { Enrollments = state.Enrollments.Remove(enrollment) };

        // 4. Register into target. If it fails, return failure (which naturally retains original state)
        var registrationResult = RegistrationService.Register(tentativeState, studentId, toSectionId);
        
        return registrationResult; 
    }
}