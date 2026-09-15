using JobOnlineAPI.Controllers;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace JobOnlineAPI.Tests
{
    public class EmployeesControllerTests
    {
        private static EmployeeSearchResponse EmptyResponse(int page, int pageSize) => new()
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            Total = 0
        };

        [Fact]
        public async Task SearchEmployees_ForwardsFiltersAndPaging_ToRepository()
        {
            var repo = new Mock<IEmployeeRepository>();
            repo.Setup(r => r.SearchEmployeesAsync("OTD", "10806", "suchart", 2, 10))
                .ReturnsAsync(EmptyResponse(2, 10));

            var controller = new EmployeesController(repo.Object);

            var result = await controller.SearchEmployees("OTD", "10806", "suchart", 2, 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<EmployeeSearchResponse>(ok.Value);
            Assert.Equal(2, response.Page);
            Assert.Equal(10, response.PageSize);
            repo.Verify(r => r.SearchEmployeesAsync("OTD", "10806", "suchart", 2, 10), Times.Once);
        }

        [Fact]
        public async Task SearchEmployees_DefaultsPageAndPageSize_WhenOmitted()
        {
            var repo = new Mock<IEmployeeRepository>();
            repo.Setup(r => r.SearchEmployeesAsync(null, null, null, 1, 20))
                .ReturnsAsync(EmptyResponse(1, 20));

            var controller = new EmployeesController(repo.Object);

            var result = await controller.SearchEmployees(null, null, null);

            Assert.IsType<OkObjectResult>(result);
            repo.Verify(r => r.SearchEmployeesAsync(null, null, null, 1, 20), Times.Once);
        }

        [Fact]
        public async Task SearchEmployees_EmptySearch_IsPassedThroughUnchanged()
        {
            var repo = new Mock<IEmployeeRepository>();
            repo.Setup(r => r.SearchEmployeesAsync(null, null, "", 1, 20))
                .ReturnsAsync(EmptyResponse(1, 20));

            var controller = new EmployeesController(repo.Object);

            var result = await controller.SearchEmployees(null, null, "");

            Assert.IsType<OkObjectResult>(result);
            repo.Verify(r => r.SearchEmployeesAsync(null, null, "", 1, 20), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task SearchEmployees_RejectsInvalidPage(int page)
        {
            var repo = new Mock<IEmployeeRepository>();
            var controller = new EmployeesController(repo.Object);

            var result = await controller.SearchEmployees(null, null, null, page, 20);

            Assert.IsType<BadRequestObjectResult>(result);
            repo.Verify(r => r.SearchEmployeesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(201)]
        public async Task SearchEmployees_RejectsInvalidPageSize(int pageSize)
        {
            var repo = new Mock<IEmployeeRepository>();
            var controller = new EmployeesController(repo.Object);

            var result = await controller.SearchEmployees(null, null, null, 1, pageSize);

            Assert.IsType<BadRequestObjectResult>(result);
            repo.Verify(r => r.SearchEmployeesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SearchEmployees_ReturnsIsAdminFlag_FromRepository()
        {
            var repo = new Mock<IEmployeeRepository>();
            repo.Setup(r => r.SearchEmployeesAsync(null, null, null, 1, 20))
                .ReturnsAsync(new EmployeeSearchResponse
                {
                    Items =
                    [
                        new EmployeeSearchResult { EmpNo = "NAT00220", NameThai = "นายสุชาติ ทวนสุวรรณ์", IsAdmin = true },
                        new EmployeeSearchResult { EmpNo = "ACT00001", NameThai = "น.ส.สุบงกช ชลิตเรืองกุล", IsAdmin = false }
                    ],
                    Page = 1,
                    PageSize = 20,
                    Total = 2
                });

            var controller = new EmployeesController(repo.Object);

            var result = await controller.SearchEmployees(null, null, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<EmployeeSearchResponse>(ok.Value);
            Assert.Equal(2, response.Total);
            Assert.True(response.Items.First(i => i.EmpNo == "NAT00220").IsAdmin);
            Assert.False(response.Items.First(i => i.EmpNo == "ACT00001").IsAdmin);
        }

        [Fact]
        public async Task SearchEmployees_ReturnsInternalServerError_WhenRepositoryThrows()
        {
            var repo = new Mock<IEmployeeRepository>();
            repo.Setup(r => r.SearchEmployeesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("linked server unavailable"));

            var controller = new EmployeesController(repo.Object);

            var result = await controller.SearchEmployees(null, null, null);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }
    }
}
