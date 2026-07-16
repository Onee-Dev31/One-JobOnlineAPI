using System.Security.Claims;
using System.Text.Json;
using JobOnlineAPI.Controllers;
using JobOnlineAPI.Models;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JobOnlineAPI.Tests;

public class ManualTraineeIntegrationTests
{
    [Fact]
    public async Task ManualTrainee_WithoutFiles_ReturnsExpectedContract()
    {
        var service = SuccessfulService();
        var controller = CreateController(service.Object, "Admin");

        var response = await Call(controller, ValidJson(), null, null, null, null);

        var ok = Assert.IsType<OkObjectResult>(response);
        var body = Assert.IsType<CreateManualTraineeResult>(ok.Value);
        Assert.Equal(123, body.Id);
        Assert.Equal(456, body.TraineeApplicationId);
        Assert.Equal(789, body.ApplicantId);
    }

    [Fact]
    public async Task ManualTrainee_WithAllFourFileSections_ForwardsExactMultipartNames()
    {
        IReadOnlyDictionary<string, IReadOnlyList<IFormFile>>? captured = null;
        var service = SuccessfulService((_, files) => captured = files);
        var controller = CreateController(service.Object, "Admin");

        await Call(controller, ValidJson(), [File("id.pdf", "application/pdf")],
            [File("house.png", "image/png")], [File("resume.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")],
            [File("transcript.jpg", "image/jpeg")]);

        Assert.NotNull(captured);
        Assert.Equal(["houseReg", "idCard", "resume", "transcript"], captured.Keys.OrderBy(x => x));
        Assert.All(captured.Values, files => Assert.Single(files));
    }

    [Fact]
    public async Task ManualTrainee_AllowsNullOptionalFields()
    {
        var service = SuccessfulService();
        var controller = CreateController(service.Object, "Admin");
        var json = ValidJson(new { NicknameT = (string?)null, DateOfBirth = (DateTime?)null, DesiredField1 = (string?)null });

        var response = await Call(controller, json, null, null, null, null);

        Assert.IsType<OkObjectResult>(response);
        service.Verify(x => x.CreateAsync(It.IsAny<ManualTraineeRequest>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<IFormFile>>>(), 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ManualTrainee_NonAdmin_ReturnsForbidden_WithoutWriting()
    {
        var service = SuccessfulService();
        var controller = CreateController(service.Object, "Applicant");

        var response = await Call(controller, ValidJson(), null, null, null, null);

        var forbidden = Assert.IsType<ObjectResult>(response);
        Assert.Equal(403, forbidden.StatusCode);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ManualTrainee_AssignmentFailure_ReturnsRequestId_AndServiceOwnsRollback()
    {
        var service = new Mock<IManualTraineeService>();
        service.Setup(x => x.CreateAsync(It.IsAny<ManualTraineeRequest>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<IFormFile>>>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("assignment insert failed"));
        var controller = CreateController(service.Object, "Admin");
        controller.HttpContext.TraceIdentifier = "test-request-id";

        var response = await Call(controller, ValidJson(), null, null, null, null);

        var error = Assert.IsType<ObjectResult>(response);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("test-request-id", JsonSerializer.Serialize(error.Value));
    }

    private static Mock<IManualTraineeService> SuccessfulService(Action<ManualTraineeRequest, IReadOnlyDictionary<string, IReadOnlyList<IFormFile>>>? capture = null)
    {
        var service = new Mock<IManualTraineeService>();
        service.Setup(x => x.CreateAsync(It.IsAny<ManualTraineeRequest>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<IFormFile>>>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback<ManualTraineeRequest, IReadOnlyDictionary<string, IReadOnlyList<IFormFile>>, int?, CancellationToken>((r, f, _, _) => capture?.Invoke(r, f))
            .ReturnsAsync(new CreateManualTraineeResult { Id = 123, TraineeApplicationId = 456, ApplicantId = 789, Quota = 5, ActiveOverlapCount = 1 });
        return service;
    }

    private static TraineeManagementController CreateController(IManualTraineeService service, string role)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Server=unused" }).Build();
        var controller = new TraineeManagementController(config, service, NullLogger<TraineeManagementController>.Instance);
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role), new Claim("admin_id", "10")], "test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
        return controller;
    }

    private static Task<IActionResult> Call(TraineeManagementController controller, string json,
        List<IFormFile>? id, List<IFormFile>? house, List<IFormFile>? resume, List<IFormFile>? transcript) =>
        controller.CreateManualTrainee(json, id, house, resume, transcript, CancellationToken.None);

    private static string ValidJson(object? extra = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["CompanyCode"] = "OTV", ["DepartmentCode"] = "15707", ["StartDate"] = "2026-07-15", ["EndDate"] = "2026-10-15",
            ["NameFirstT"] = "ทดสอบ", ["NameLastT"] = "ระบบ", ["Mobile"] = "0800000000", ["Email"] = "test@example.com", ["School"] = "มหาวิทยาลัยทดสอบ"
        };
        if (extra != null) foreach (var p in extra.GetType().GetProperties()) values[p.Name] = p.GetValue(extra);
        return JsonSerializer.Serialize(values);
    }

    private static IFormFile File(string name, string contentType) =>
        new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", name) { Headers = new HeaderDictionary(), ContentType = contentType };
}
