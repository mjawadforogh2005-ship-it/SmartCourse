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
    }

    private static Result<RegistrationState> ValidateStructure(RegistrationState state)
    {
        if (state.Students.Any(s => string.IsNullOrWhiteSpace(s.Id)) || state.Students.Select(s => s.Id).Distinct().Count() != state.Students.Length) return new Result<RegistrationState>.Failure("Invalid student IDs.");
        if (state.Courses.Any(c => string.IsNullOrWhiteSpace(c.Id)) || state.Courses.Select(c => c.Id).Distinct().Count() != state.Courses.Length) return new Result<RegistrationState>.Failure("Invalid course IDs.");
        if (state.Sections.Any(s => string.IsNullOrWhiteSpace(s.Id)) || state.Sections.Select(s => s.Id).Distinct().Count() != state.Sections.Length) return new Result<RegistrationState>.Failure("Invalid section IDs.");
        if (state.Courses.Any(c => c.Credits <= 0)) return new Result<RegistrationState>.Failure("Course credits must be positive.");
        if (state.Sections.Any(s => s.Capacity < 0) || state.Students.Any(s => s.CreditLimit < 0)) return new Result<RegistrationState>.Failure("Capacity/Limits must be non-negative.");
        
        foreach (var section in state.Sections) 
            if (!state.Courses.Any(c => c.Id == section.CourseId)) return new Result<RegistrationState>.Failure("Missing course ref.");
            
        foreach (var enrollment in state.Enrollments)
        {
            if (!state.Students.Any(s => s.Id == enrollment.StudentId)) return new Result<RegistrationState>.Failure("Missing student ref.");
            if (!state.Sections.Any(s => s.Id == enrollment.SectionId)) return new Result<RegistrationState>.Failure("Missing section ref.");
        }
        
        var studentCourses = state.Enrollments.Select(e => new { e.StudentId, CourseId = state.Sections.First(s => s.Id == e.SectionId).CourseId }).ToList();
        if (studentCourses.Distinct().Count() != studentCourses.Count) return new Result<RegistrationState>.Failure("Duplicate course enrollment.");
        
        foreach (var section in state.Sections) 
            if (section.Schedule.Any(slot => slot.Start >= slot.End)) return new Result<RegistrationState>.Failure("Meeting start >= end.");
            
        return new Result<RegistrationState>.Success(state);
    }
}