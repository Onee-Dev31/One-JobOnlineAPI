using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobOnlineAPI.Models
{
    public class TraineeApplication
    {
        public int TraineeApplicationID { get; set; }
        // Part1 JobSlotAssignments.AssignmentID, when this submission continues one.
        public int? AssignmentID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? DesiredField1 { get; set; }
        public string? DesiredField2 { get; set; }
        public string? DesiredField3 { get; set; }
        public string? InternshipType { get; set; }
        public string? Reason { get; set; }
        public string? ReasonOther { get; set; }

        // Thai name
        public string? PrefixT { get; set; }
        public required string NameFirstT { get; set; }
        public required string NameLastT { get; set; }
        public string? NicknameT { get; set; }

        // English name
        public string? PrefixE { get; set; }
        public string? NameFirstE { get; set; }
        public string? NameLastE { get; set; }
        public string? NicknameE { get; set; }

        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? Race { get; set; }
        public string? Religion { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string? IDCardNo { get; set; }
        public string? IDIssuedBy { get; set; }
        public DateTime? IDExpiredDate { get; set; }
        public string? Address { get; set; }
        public int? ProvinceID { get; set; }
        public int? DistrictID { get; set; }
        public int? SubDistrictID { get; set; }
        public string? PostalCode { get; set; }
        public string? Telephone { get; set; }
        public required string Mobile { get; set; }
        public required string Email { get; set; }
        public string? FatherName { get; set; }
        public string? FatherOccupation { get; set; }
        public string? FatherStatus { get; set; }
        public string? MotherName { get; set; }
        public string? MotherOccupation { get; set; }
        public string? MotherStatus { get; set; }
        public int? SiblingCount { get; set; }
        public int? SiblingOrder { get; set; }
        public string? EmergencyName { get; set; }
        public string? EmergencyRelation { get; set; }
        public string? EmergencyAddress { get; set; }
        public string? EmergencyPhone { get; set; }
        public required string School { get; set; }
        public string? Faculty { get; set; }
        public string? Major { get; set; }
        public string? Minor { get; set; }
        public string? YearOfStudy { get; set; }
        public string? AdvisorName { get; set; }
        public string? AdvisorPhone { get; set; }
        public string? Activities { get; set; }
        // Frontend sends this as a checkbox multi-select (JSON array) or, in older flows, a plain
        // string. The NVARCHAR(200) column stores whichever form was sent as JSON text (see the
        // ISJSON check on the legacy TraineeApplications table), so both shapes are accepted here.
        [JsonConverter(typeof(InfoSourcesJsonConverter))]
        public string? InfoSources { get; set; }
        public string? InfoSourceStaffName { get; set; }
        public string? InfoSourceDepartment { get; set; }
        public string? InfoSourceOther { get; set; }
        public string Status { get; set; } = "Pending HR Screening";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // Null for walk-in entries not tied to a specific job posting (e.g. manual entry
        // straight into a department via TraineeManagement, not applying to a listed job).
        public int? JobID { get; set; }
    }

    public class TraineeFile
    {
        public int FileID { get; set; }
        public int TraineeApplicationID { get; set; }
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string? FileType { get; set; }
        public string? Description { get; set; }
        public string? SectionFile { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    public class TraineeApplicationDetail : TraineeApplication
    {
        public List<TraineeFile> Files { get; set; } = [];
    }

    // Accepts InfoSources as either a JSON string or a JSON array of strings; arrays are
    // re-encoded to JSON text so the value still fits the single NVARCHAR(200) SP parameter.
    public sealed class InfoSourcesJsonConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.String) return reader.GetString();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var items = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
                return JsonSerializer.Serialize(items);
            }

            throw new JsonException($"infoSources: unexpected token {reader.TokenType}, expected string or array of strings.");
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}
