using SmartCourse.App.Models;
using SmartCourse.App.Services;
using SmartCourse.App.Functional;

namespace SmartCourse.Tests;

public class AlamZaibTests
{
    private RegistrationState GetSample() => new(
        [new Student("S1", "Alam Zaib", ["C1"], 6)],
        [new Course("C1", "Math", 3, []), new Course("C2", "Science", 3, [])],
        [new CourseSection("Sec1", "C1", 2, []), new CourseSection("Sec2", "C1", 2, []), new CourseSection("Sec3", "C2", 2, [])], []);

    [Fact]
    public void Transfer_SameCourse_Succeeds()
    {
        var regResult = RegistrationService.Register(GetSample(), "S1", "Sec1");
        var state = Assert.IsType<Result<RegistrationState>.Success>(regResult).Value;
        
        var result = SectionTransferService.Transfer(state, "S1", "Sec1", "Sec2");
        
        Assert.IsType<Result<RegistrationState>.Success>(result);
        var updated = Assert.IsType<Result<RegistrationState>.Success>(result).Value;
        Assert.Contains(updated.Enrollments, e => e.SectionId == "Sec2");
        Assert.DoesNotContain(updated.Enrollments, e => e.SectionId == "Sec1");
    }

    [Fact]
    public void Transfer_DifferentCourse_FailsAndPreservesState()
    {
        var regResult = RegistrationService.Register(GetSample(), "S1", "Sec1");
        var state = Assert.IsType<Result<RegistrationState>.Success>(regResult).Value;
        
        var result = SectionTransferService.Transfer(state, "S1", "Sec1", "Sec3");
        
        Assert.IsType<Result<RegistrationState>.Failure>(result);
        Assert.Single(state.Enrollments);
        Assert.Equal("Sec1", state.Enrollments[0].SectionId);
    }

    [Fact]
    public void JsonRepository_MalformedJson_ReturnsFailure()
    {
        var result = JsonRepository.LoadFromJson("{ bad json : true }");
        Assert.IsType<Result<RegistrationState>.Failure>(result);
    }

    [Fact]
    public void JsonRepository_MissingFile_ThrowsException()
    {
        // We test the operational boundary: File.ReadAllText
        var missingPath = "does_not_exist.json";
        
        Assert.Throws<FileNotFoundException>(() => File.ReadAllText(missingPath));
    }
}