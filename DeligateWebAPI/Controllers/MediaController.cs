using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Xabe.FFmpeg;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MediaController(
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<ActionResult<Media>> PostMedia([FromForm] MediaUploadRequest request)
        {
            // Validation
            if (request?.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest("Username is required");

            var isImage = request.File.ContentType.StartsWith("image/");
            var isVideo = request.File.ContentType.StartsWith("video/");

            //if (!isImage && !isVideo)
            //    return BadRequest("Only images and videos are allowed");

            var fileExtension = Path.GetExtension(request.File.FileName).ToLower();
            bool isWebp = fileExtension == ".webp";

            if (!isImage && !isVideo && !isWebp)
                return BadRequest("Only images and videos are allowed");

            try
            {
                // Create upload directories
                var mediaTypeFolder = isImage ? "Images" : "Videos";
                var uploadsPath = Path.Combine(_env.WebRootPath, "MediaUploads", mediaTypeFolder);
                var thumbnailsPath = Path.Combine(_env.WebRootPath, "MediaUploads", "Thumbnails");

                Directory.CreateDirectory(uploadsPath);
                Directory.CreateDirectory(thumbnailsPath);

                // Generate filename and path
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                string thumbnailPath = null;
                string thumbnailRelativePath = null;

                if (isVideo)
                {
                    var tempFilePath = Path.GetTempFileName();
                    try
                    {
                        // Save uploaded file to temp location
                        using (var tempStream = new FileStream(tempFilePath, FileMode.Create))
                        {
                            await request.File.CopyToAsync(tempStream);
                        }

                        // Get video duration and validate
                        var mediaInfo = await FFmpeg.GetMediaInfo(tempFilePath);
                        double duration = mediaInfo.Duration.TotalSeconds;

                        if (duration > 60)
                        {
                            return BadRequest("Video exceeds 1 minute.");
                        }
  

                        // Move valid video to final location
                        System.IO.File.Move(tempFilePath, filePath, overwrite: true);

                        // Generate thumbnail
                        var thumbnailResult = await GenerateVideoThumbnail(filePath, thumbnailsPath, fileName);
                        if (thumbnailResult.Success)
                        {
                            thumbnailPath = thumbnailResult.ThumbnailPath;
                            thumbnailRelativePath = thumbnailResult.RelativePath;
                        }
                    }
                    finally
                    {
                        // Clean up temp file
                        if (System.IO.File.Exists(tempFilePath))
                            System.IO.File.Delete(tempFilePath);
                    }
                }
                else
                {
                    // Save image directly
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.File.CopyToAsync(stream);
                    }
                }

                // Create database record
                var media = new Media
                {
                    UserName = request.UserName,
                    ImagePath = isImage ? $"/MediaUploads/{mediaTypeFolder}/{fileName}" : null,
                    VideoPath = isVideo ? $"/MediaUploads/{mediaTypeFolder}/{fileName}" : null,
                    ThumbnailPath = thumbnailRelativePath
                };

                _context.Medias.Add(media);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetMedia", new { id = media.Id }, media);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<ThumbnailResult> GenerateVideoThumbnail(string videoPath, string thumbnailsPath, string originalFileName)
        {
            try
            {
                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_thumb.jpg";
                var thumbnailFullPath = Path.Combine(thumbnailsPath, thumbnailFileName);
                var thumbnailRelativePath = $"/MediaUploads/Thumbnails/{thumbnailFileName}";

                // Generate thumbnail at 3 seconds into the video (or 50% if video is shorter than 6 seconds)
                var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
                var timeSpan = mediaInfo.Duration.TotalSeconds > 6 ? TimeSpan.FromSeconds(3) : TimeSpan.FromSeconds(mediaInfo.Duration.TotalSeconds * 0.5);

                var conversion = await FFmpeg.Conversions.FromSnippet
                    .Snapshot(videoPath, thumbnailFullPath, timeSpan);

                // Set thumbnail quality and size
                //conversion.Set(VideoSize.Hd480);
                //conversion.SetOutputFormat(Format.p);

                await conversion.Start();

                return new ThumbnailResult
                {
                    Success = true,
                    ThumbnailPath = thumbnailFullPath,
                    RelativePath = thumbnailRelativePath
                };
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the upload
                Console.WriteLine($"Failed to generate thumbnail: {ex.Message}");
                return new ThumbnailResult { Success = false };
            }
        }

        // GET: api/Media
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Media>>> GetMedias()
        {
            return await _context.Medias.ToListAsync();
        }

        // GET: api/Media/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Media>> GetMedia(int id)
        {
            var media = await _context.Medias.FindAsync(id);

            if (media == null)
            {
                return NotFound();
            }

            return media;
        }

        // GET: api/Media/user/{username}
        [HttpGet("user/{username}")]
        public async Task<ActionResult<IEnumerable<Media>>> GetMediasByUser(string username)
        {
            return await _context.Medias
                .Where(m => m.UserName == username)
                .ToListAsync();
        }

        // PUT: api/Media/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMedia(int id, Media media)
        {
            if (id != media.Id)
            {
                return BadRequest();
            }

            _context.Entry(media).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MediaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Media/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.Medias.FindAsync(id);
            if (media == null)
            {
                return NotFound();
            }

            try
            {
                // Delete the physical files
                var filesToDelete = new List<string>();

                if (!string.IsNullOrEmpty(media.ImagePath))
                {
                    filesToDelete.Add(Path.Combine(_env.WebRootPath, media.ImagePath.TrimStart('/')));
                }

                if (!string.IsNullOrEmpty(media.VideoPath))
                {
                    filesToDelete.Add(Path.Combine(_env.WebRootPath, media.VideoPath.TrimStart('/')));
                }

                if (!string.IsNullOrEmpty(media.ThumbnailPath))
                {
                    filesToDelete.Add(Path.Combine(_env.WebRootPath, media.ThumbnailPath.TrimStart('/')));
                }

                // Delete all files
                foreach (var filePath in filesToDelete)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Remove from database
                _context.Medias.Remove(media);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting media: {ex.Message}");
            }
        }

        private bool MediaExists(int id)
        {
            return _context.Medias.Any(e => e.Id == id);
        }
    }

    // Helper class for thumbnail generation result
    public class ThumbnailResult
    {
        public bool Success { get; set; }
        public string ThumbnailPath { get; set; }
        public string RelativePath { get; set; }
    }
}