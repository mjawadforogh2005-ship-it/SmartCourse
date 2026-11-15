using System.Text.Json;
using SmartCourse.App.Functional;
using SmartCourse.App.Models;

namespace SmartCourse.App.Services;

public static class JsonRepository
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static Result<string> SaveToJson(RegistrationState state)
    {
        try { return new Result<string>.Success(JsonSerializer.Serialize(state, Options)); }
        catch (Exception ex) { return new Result<string>.Failure($"Serialization failed: {ex.Message}"); }
    }

    public static Result<RegistrationState> LoadFromJson(string json)
    {
        try
        {
            var state = JsonSerializer.Deserialize<RegistrationState>(json);
            if (state is null) return new Result<RegistrationState>.Failure("JSON deserialized to null.");
            return ValidateStructure(state);
        }
        catch (JsonException ex) { return new Result<RegistrationState>.Failure($"Invalid JSON format: {ex.Message}"); }
        catch (Exception ex) { return new Result<RegistrationState>.Failure($"Unexpected error: {ex.Message}"); }
    }

    private static Result<RegistrationState> ValidateStructure(RegistrationState state)
    {
        // FIX 2: ImmutableArrays are structs. Use IsDefault to check for missing collections.
        if (state.Students.IsDefault || state.Courses.IsDefault || state.Sections.IsDefault || state.Enrollments.IsDefault)
            return new Result<RegistrationState>.Failure("JSON is missing required collections (Students, Courses, Sections, Enrollments).");

        // Validate Students
        if (state.Students.Any(s => s == null || string.IsNullOrWhiteSpace(s.Id)) || state.Students.Select(s => s?.Id).Distinct().Count() != state.Students.Length) 
            return new Result<RegistrationState>.Failure("Invalid student IDs (blank or duplicate).");
            
        // Validate Courses
        if (state.Courses.Any(c => c == null || string.IsNullOrWhiteSpace(c.Id)) || state.Courses.Select(c => c?.Id).Distinct().Count() != state.Courses.Length) 
            return new Result<RegistrationState>.Failure("Invalid course IDs.");
            
        // Validate Sections
        if (state.Sections.Any(s => s == null || string.IsNullOrWhiteSpace(s.Id)) || state.Sections.Select(s => s?.Id).Distinct().Count() != state.Sections.Length) 
            return new Result<RegistrationState>.Failure("Invalid section IDs.");

        // Validate numbers
        if (state.Courses.Any(c => c != null && c.Credits <= 0)) 
            return new Result<RegistrationState>.Failure("Course credits must be positive.");
            
        if (state.Sections.Any(s => s != null && s.Capacity < 0) || state.Students.Any(s => s != null && s.CreditLimit < 0)) 
            return new Result<RegistrationState>.Failure("Capacity/Limits must be non-negative.");

        // Validate Foreign Keys (Sections -> Courses)
        foreach (var section in state.Sections)
        {
            if (section == null) continue;
            if (!state.Courses.Any(c => c != null && c.Id == section.CourseId)) 
                return new Result<RegistrationState>.Failure($"Section {section.Id} missing course ref.");
        }

        // Validate Foreign Keys (Enrollments -> Students & Sections)
        foreach (var enrollment in state.Enrollments)
        {
            if (enrollment == null) return new Result<RegistrationState>.Failure("Null enrollment found.");
            if (!state.Students.Any(s => s != null && s.Id == enrollment.StudentId)) 
                return new Result<RegistrationState>.Failure("Missing student ref.");
            if (!state.Sections.Any(s => s != null && s.Id == enrollment.SectionId)) 
                return new Result<RegistrationState>.Failure("Missing section ref.");
        }

        // Validate Unique Course Enrollment
        var studentCourses = state.Enrollments
            .Where(e => e != null)
            .Select(e => new { e.StudentId, CourseId = state.Sections.FirstOrDefault(s => s != null && s.Id == e.SectionId)?.CourseId })
            .ToList();
            
        if (studentCourses.Distinct().Count() != studentCourses.Count) 
            return new Result<RegistrationState>.Failure("Duplicate course enrollment.");

        // Validate Meeting Times
        foreach (var section in state.Sections)
        {
            if (section == null) continue;
            if (section.Schedule.Any(slot => slot != null && slot.Start >= slot.End)) 
                return new Result<RegistrationState>.Failure("Meeting start >= end.");
        }
            
        return new Result<RegistrationState>.Success(state);
    }
}