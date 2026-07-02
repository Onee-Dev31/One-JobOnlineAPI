using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TraineeApplicationsController(DapperContext context) : ControllerBase
    {
        private readonly DapperContext _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            using var conn = _context.CreateConnection();
            var result = await conn.QueryAsync<TraineeApplication>(
                "sp_GetTraineeApplications",
                new { Status = status },
                commandType: CommandType.StoredProcedure);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = _context.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetTraineeApplicationByID",
                new { TraineeApplicationID = id },
                commandType: CommandType.StoredProcedure);

            var application = await multi.ReadFirstOrDefaultAsync<TraineeApplication>();
            if (application == null)
                return NotFound(new { message = "ไม่พบข้อมูลใบสมัครฝึกงานนี้" });

            var files = (await multi.ReadAsync<TraineeFile>()).ToList();

            return Ok(new TraineeApplicationDetail
            {
                TraineeApplicationID = application.TraineeApplicationID,
                StartDate = application.StartDate,
                EndDate = application.EndDate,
                DesiredField1 = application.DesiredField1,
                DesiredField2 = application.DesiredField2,
                DesiredField3 = application.DesiredField3,
                Reason = application.Reason,
                ReasonOther = application.ReasonOther,
                Name = application.Name,
                Surname = application.Surname,
                Nickname = application.Nickname,
                DateOfBirth = application.DateOfBirth,
                Age = application.Age,
                PlaceOfBirth = application.PlaceOfBirth,
                Nationality = application.Nationality,
                Race = application.Race,
                Religion = application.Religion,
                Height = application.Height,
                Weight = application.Weight,
                IDCardNo = application.IDCardNo,
                IDIssuedBy = application.IDIssuedBy,
                IDExpiredDate = application.IDExpiredDate,
                Address = application.Address,
                ProvinceID = application.ProvinceID,
                DistrictID = application.DistrictID,
                SubDistrictID = application.SubDistrictID,
                PostalCode = application.PostalCode,
                Telephone = application.Telephone,
                Mobile = application.Mobile,
                Email = application.Email,
                FatherName = application.FatherName,
                FatherOccupation = application.FatherOccupation,
                FatherStatus = application.FatherStatus,
                MotherName = application.MotherName,
                MotherOccupation = application.MotherOccupation,
                MotherStatus = application.MotherStatus,
                SiblingCount = application.SiblingCount,
                SiblingOrder = application.SiblingOrder,
                EmergencyName = application.EmergencyName,
                EmergencyRelation = application.EmergencyRelation,
                EmergencyAddress = application.EmergencyAddress,
                EmergencyPhone = application.EmergencyPhone,
                School = application.School,
                Faculty = application.Faculty,
                Major = application.Major,
                Minor = application.Minor,
                YearOfStudy = application.YearOfStudy,
                AdvisorName = application.AdvisorName,
                AdvisorPhone = application.AdvisorPhone,
                Activities = application.Activities,
                InfoSources = application.InfoSources,
                InfoSourceStaffName = application.InfoSourceStaffName,
                InfoSourceDepartment = application.InfoSourceDepartment,
                InfoSourceOther = application.InfoSourceOther,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt,
                Files = files
            });
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] TraineeApplication request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = _context.CreateConnection();
            var id = await conn.ExecuteScalarAsync<int>(
                "usp_TraineeApplications_Upsert",
                new
                {
                    request.TraineeApplicationID,
                    request.StartDate,
                    request.EndDate,
                    request.DesiredField1,
                    request.DesiredField2,
                    request.DesiredField3,
                    request.Reason,
                    request.ReasonOther,
                    request.Name,
                    request.Surname,
                    request.Nickname,
                    request.DateOfBirth,
                    request.Age,
                    request.PlaceOfBirth,
                    request.Nationality,
                    request.Race,
                    request.Religion,
                    request.Height,
                    request.Weight,
                    request.IDCardNo,
                    request.IDIssuedBy,
                    request.IDExpiredDate,
                    request.Address,
                    request.ProvinceID,
                    request.DistrictID,
                    request.SubDistrictID,
                    request.PostalCode,
                    request.Telephone,
                    request.Mobile,
                    request.Email,
                    request.FatherName,
                    request.FatherOccupation,
                    request.FatherStatus,
                    request.MotherName,
                    request.MotherOccupation,
                    request.MotherStatus,
                    request.SiblingCount,
                    request.SiblingOrder,
                    request.EmergencyName,
                    request.EmergencyRelation,
                    request.EmergencyAddress,
                    request.EmergencyPhone,
                    request.School,
                    request.Faculty,
                    request.Major,
                    request.Minor,
                    request.YearOfStudy,
                    request.AdvisorName,
                    request.AdvisorPhone,
                    request.Activities,
                    request.InfoSources,
                    request.InfoSourceStaffName,
                    request.InfoSourceDepartment,
                    request.InfoSourceOther,
                    request.Status
                },
                commandType: CommandType.StoredProcedure);

            return Ok(new { TraineeApplicationID = id });
        }
    }
}
