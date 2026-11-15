using SmartCourse.App.Models;
using SmartCourse.App.Services;
using SmartCourse.App.Functional;

// 1. Look for the file inside the Data folder
var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "SmartCourse.App", "Data", "sample_state.json");
var json = File.ReadAllText(jsonPath);
var loadResult = JsonRepository.LoadFromJson(json);

if (loadResult is Result<RegistrationState>.Failure fail)
{
    Console.WriteLine($"Failed to load data: {fail.Error}");
    return;
}

// 2. Initialize state from JSON
var state = ((Result<RegistrationState>.Success)loadResult).Value;
Console.WriteLine("SmartCourse demo: in-memory data loaded from JSON.");
Console.WriteLine("Students S001 (Mohammad) / S002 (Afridi). Sections A (Programming), B (requires CS100).");

while (true)
{
    Console.WriteLine("\n1 Register | 2 Drop | 3 Transfer | 4 Reports | 5 Enrollments | 0 Exit");
    var choice = Console.ReadLine();
    if (choice is null or "0") break;
    
    if (choice == "5")
    {
        if (state.Enrollments.IsEmpty) Console.WriteLine("No enrollments.");
        foreach (var e in state.Enrollments) Console.WriteLine($"{e.StudentId} -> {e.SectionId}");
        continue;
    }
    
    if (choice == "4")
    {
        Console.WriteLine("--- Course Enrollment Summary ---");
        foreach (var report in CourseReports.GetCourseEnrollmentSummary(state))
        {
            // Safely extract the properties from the anonymous object
            var courseId = report.GetType().GetProperty("CourseId")?.GetValue(report, null);
            var totalEnrollments = report.GetType().GetProperty("TotalEnrollments")?.GetValue(report, null);
            var capacity = report.GetType().GetProperty("Capacity")?.GetValue(report, null);
            
            Console.WriteLine($"Course {courseId}: {totalEnrollments} enrolled / {capacity} total capacity");
        }
            
        Console.WriteLine("--- Student Credit Loads ---");
        foreach (var report in CourseReports.GetStudentCreditLoads(state))
            Console.WriteLine($"Student {report.StudentId}: {report.TotalCredits} credits");
        continue;
    }

    if (choice is not ("1" or "2" or "3")) { Console.WriteLine("Invalid option."); continue; }
    
    Console.Write("Student ID: "); var studentId = Console.ReadLine()?.Trim().ToUpperInvariant();
    
    if (choice == "3")
    {
        Console.Write("From Section ID: "); var fromId = Console.ReadLine()?.Trim().ToUpperInvariant();
        Console.Write("To Section ID: "); var toId = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (studentId is null || fromId is null || toId is null) break;
        
        var result = SectionTransferService.Transfer(state, studentId, fromId, toId);
        switch (result)
        {
            case Result<RegistrationState>.Success(var updated): state = updated; Console.WriteLine("Transfer successful."); break;
            case Result<RegistrationState>.Failure(var error): Console.WriteLine($"Rejected: {error}"); break;
        }
        continue;
    }

    Console.Write("Section ID: "); var sectionId = Console.ReadLine()?.Trim().ToUpperInvariant();
    if (studentId is null || sectionId is null) break;
    
    var regResult = choice == "1" ? RegistrationService.Register(state, studentId, sectionId) : RegistrationService.Drop(state, studentId, sectionId);
    switch (regResult)
    {
        case Result<RegistrationState>.Success(var updated): state = updated; Console.WriteLine("Success."); break;
        case Result<RegistrationState>.Failure(var error): Console.WriteLine($"Rejected: {error}"); break;
    }
}