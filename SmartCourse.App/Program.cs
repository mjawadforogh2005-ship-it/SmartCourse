using SmartCourse.App.Models;
using SmartCourse.App.Services;
using SmartCourse.App.Functional;

// --data allows an isolated demo/test file. Normal runs use a stable per-user save file.
if (args.Length != 0 && (args.Length != 2 || args[0] != "--data"))
{
    Console.WriteLine("Usage: SmartCourse.App [--data path-to-json]");
    Environment.ExitCode = 1;
    return;
}
var dataPath = args.Length == 2 ? args[1] : Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartCourse", "state.json");
var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "sample_state.json");
var loaded = File.Exists(dataPath) ? LoadData(dataPath)
    : args.Length == 2 ? LoadData(dataPath) : LoadData(seedPath);
if (loaded is Result<RegistrationState>.Failure loadError)
{
    Console.WriteLine($"Cannot start: {loadError.Error}");
    Environment.ExitCode = 1;
    return;
}
var state = ((Result<RegistrationState>.Success)loaded).Value;
Console.WriteLine("SmartCourse — Course Registration");
Console.WriteLine($"Save file: {dataPath}");
Console.WriteLine("Changes are saved after every successful registration, drop, or transfer.");

string? ReadId(string label)
{
    Console.Write(label);
    return Console.ReadLine()?.Trim();
}
string StudentId(string id) => state.Students.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Id ?? id;
string SectionId(string id) => state.Sections.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Id ?? id;
void Apply(Result<RegistrationState> result)
{
    if (result is Result<RegistrationState>.Failure error) { Console.WriteLine($"Rejected: {error.Error}"); return; }
    var next = ((Result<RegistrationState>.Success)result).Value;
    switch (SaveData(next, dataPath))
    {
        case Result<RegistrationState>.Success:
            state = next;
            Console.WriteLine("Success. Changes saved.");
            break;
        case Result<RegistrationState>.Failure saveError:
            Console.WriteLine($"Change not applied: {saveError.Error}");
            break;
    }
}
while (true)
{
    Console.WriteLine("\n1 Register | 2 Drop | 3 Transfer | 4 Reports | 5 Enrollments");
    Console.WriteLine("6 Plan courses | 7 Check credits | 8 Catalog | 0 Exit");
    var choice = Console.ReadLine()?.Trim();
    if (choice is null or "0") break;
    if (choice == "") continue;
    if (choice == "8")
    {
        foreach (var student in state.Students) Console.WriteLine($"{student.Id}: {student.Name}, credit limit {student.CreditLimit}");
        foreach (var section in state.Sections)
        {
            var course = state.Courses.First(c => c.Id == section.CourseId);
            Console.WriteLine($"{section.Id}: {course.Title}, {course.Credits} credits, {state.Enrollments.Count(e => e.SectionId == section.Id)}/{section.Capacity} seats used; prerequisites: {string.Join(", ", course.PrerequisiteCourseIds)}");
            foreach (var slot in section.Schedule) Console.WriteLine($"  {slot.Day} {slot.Start:HH:mm}-{slot.End:HH:mm}");
        }
        continue;
    }
    if (choice == "4")
    {
        Console.WriteLine("Course enrollment summary:");
        foreach (var row in CourseReports.GetCourseEnrollmentSummary(state)) Console.WriteLine(row);
        Console.WriteLine("Student credit loads:");
        foreach (var row in CourseReports.GetStudentCreditLoads(state)) Console.WriteLine($"{row.StudentId}: {row.TotalCredits} credits");
        continue;
    }
    if (choice == "5")
    {
        if (state.Enrollments.IsEmpty) Console.WriteLine("No enrollments.");
        foreach (var e in state.Enrollments) Console.WriteLine($"{e.StudentId} -> {e.SectionId}");
        continue;
    }
    if (choice is not ("1" or "2" or "3" or "6" or "7")) { Console.WriteLine("Choose a listed option."); continue; }
    var input = ReadId("Student ID: ");
    if (input is null) break;
    var studentId = StudentId(input);
    if (!state.Students.Any(s => s.Id == studentId)) { Console.WriteLine("Student not found."); continue; }
    if (choice == "6")
    {
        var recommendations = AcademicPlanningService.RecommendCourses(state, studentId).ToList();
        if (recommendations.Count == 0) Console.WriteLine("No eligible courses with available, conflict-free sections.");
        foreach (var course in recommendations) Console.WriteLine($"{course.Id}: {course.Title} ({course.Credits} credits)");
        Console.WriteLine("Each recommendation is evaluated separately against the current schedule.");
        continue;
    }
    if (choice == "7")
    {
        Console.Write("Additional credits: "); var creditsText = Console.ReadLine();
        if (creditsText is null) break;
        if (!int.TryParse(creditsText, out var credits)) { Console.WriteLine("Enter a positive whole number."); continue; }
        if (credits <= 0) Console.WriteLine("Additional credits must be positive.");
        else if (AcademicPlanningService.HasRemainingCreditCapacity(state, studentId, credits))
            Console.WriteLine("The proposed credits fit within your limit.");
        else Console.WriteLine("Credit limit exceeded.");
        continue;
    }
    var first = ReadId(choice == "3" ? "From section ID: " : "Section ID: ");
    if (first is null) break;
    var sectionId = SectionId(first);
    if (choice == "3")
    {
        var second = ReadId("To section ID: "); if (second is null) break;
        Apply(SectionTransferService.Transfer(state, studentId, sectionId, SectionId(second)));
    }
    else
    {
        // Use the curried function here to apply strict prerequisite policy
        var strictPolicy = EligibilityPolicies.MakeStrictPrerequisitePolicy();
        Apply(choice == "1" ? RegistrationService.Register(state, studentId, sectionId, strictPolicy) : RegistrationService.Drop(state, studentId, sectionId));
    }
}

static Result<RegistrationState> LoadData(string path)
{
    try { return JsonRepository.LoadFromJson(File.ReadAllText(path)); }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
    { return new Result<RegistrationState>.Failure($"Cannot read data file: {ex.Message}"); }
}

static Result<RegistrationState> SaveData(RegistrationState next, string path)
{
    var serialized = JsonRepository.SaveToJson(next);
    if (serialized is Result<string>.Failure failure) return new Result<RegistrationState>.Failure(failure.Error);
    string? temporary = null;
    try
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, ((Result<string>.Success)serialized).Value);
        File.Move(temporary, fullPath, overwrite: true);
        return new Result<RegistrationState>.Success(next);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
    { return new Result<RegistrationState>.Failure($"Cannot save data file: {ex.Message}"); }
    finally
    {
        if (temporary is not null)
            try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}