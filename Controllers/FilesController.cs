using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.Filters;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/files")]
    [JwtAuthorize]
    public class FilesController(IWebHostEnvironment env) : ControllerBase
    {
        private readonly string _appFilesRoot = Path.GetFullPath(
            Path.Combine(env.ContentRootPath, "AppFiles"));

        /// <summary>
        /// ดาวน์โหลดไฟล์จาก AppFiles (ต้องมี JWT token)
        /// </summary>
        [HttpGet("{*filePath}")]
        public IActionResult GetFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return BadRequest();

            // ป้องกัน path traversal — fullPath ต้องอยู่ภายใน _appFilesRoot เท่านั้น
            var fullPath = Path.GetFullPath(Path.Combine(_appFilesRoot, filePath));
            if (!fullPath.StartsWith(_appFilesRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return BadRequest();

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
            {
                ".pdf"  => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc"  => "application/msword",
                _       => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType);
        }
    }
}
