using SmartCourse.App.Models;
using SmartCourse.App.Services;
using SmartCourse.App.Functional;

var state = new RegistrationState(
    [new Student("S001", "Mohammad", ["CS100"], 6), new Student("S002", "Afridi", [], 6)],
    [new Course("CS100", "Programming", 3, []), new Course("CS200", "Functional Programming", 3, ["CS100"])],
    [new CourseSection("A", "CS100", 2, []), new CourseSection("B", "CS200", 2, [])], []);
Console.WriteLine("SmartCourse demo: in-memory data resets on exit. Students S001/S002. Sections A (Programming), B (requires CS100).");
while (true)
{
    Console.WriteLine("1 Register | 2 Drop | 3 Enrollments | 0 Exit");
    var choice = Console.ReadLine();
    if (choice is null or "0") break;
    if (choice == "3")
    {
        if (state.Enrollments.IsEmpty) Console.WriteLine("No enrollments.");
        foreach (var e in state.Enrollments) Console.WriteLine($"{e.StudentId} -> {e.SectionId}");
        continue;
    }
    if (choice is not ("1" or "2")) { Console.WriteLine("Invalid option."); continue; }
    Console.Write("Student ID: "); var studentId = Console.ReadLine()?.Trim().ToUpperInvariant();
    Console.Write("Section ID: "); var sectionId = Console.ReadLine()?.Trim().ToUpperInvariant();
    if (studentId is null || sectionId is null) break;
    var result = choice == "1" ? RegistrationService.Register(state, studentId, sectionId) : RegistrationService.Drop(state, studentId, sectionId);
    switch (result)
    {
        case Result<RegistrationState>.Success(var updated): state = updated; Console.WriteLine("Success."); break;
        case Result<RegistrationState>.Failure(var error): Console.WriteLine($"Rejected: {error}"); break;
    }
}
