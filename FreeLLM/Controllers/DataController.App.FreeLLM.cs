using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace FreeLLM.Server.Controllers;

// Use this file as a place to put any application-specific API endpoints.

public partial class DataController
{
    #region File Operations

    // POST: api/Data/GetFileContents
    // Retrieves full file contents for a given list of files.
    [HttpPost($"~/{DataObjects.Endpoints.EndpointFileGpt.GetFileContents}")]
    [Authorize(Policy = Policies.AppAdmin)]
    public async Task<IActionResult> GetFileContents([FromBody] DataObjects.FileContentRequest request)
    {
        var results = new List<DataObjects.FileContentItem>();

        foreach (var path in request.FilePaths) {
            if (System.IO.File.Exists(path)) {
                string content;
                // Create a composite key using a fixed identifier and the file path.
                string cacheKey = $"FileContent_{path}";
                if (!_cache.TryGetValue(cacheKey, out content)) {
                    content = await System.IO.File.ReadAllTextAsync(path);
                    _cache.Set(cacheKey, content, TimeSpan.FromMinutes(5));
                }
                results.Add(new DataObjects.FileContentItem {
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                    Content = content
                });
            }
        }

        return Ok(results);
    }

    // POST: api/Data/GetFileMetadata
    // Retrieves only metadata (line count and character count) for a given list of files.
    [HttpPost($"~/{DataObjects.Endpoints.EndpointFileGpt.GetFileMetadata}")]
    [Authorize(Policy = Policies.AppAdmin)]
    public async Task<IActionResult> GetFileMetadata([FromBody] DataObjects.FileContentRequest request)
    {
        var results = new List<DataObjects.FileMetadataItem>();

        foreach (var path in request.FilePaths) {
            if (System.IO.File.Exists(path)) {
                string content;
                // Use the same composite key as above.
                string cacheKey = $"FileContent_{path}";
                if (!_cache.TryGetValue(cacheKey, out content)) {
                    content = await System.IO.File.ReadAllTextAsync(path);
                    _cache.Set(cacheKey, content, TimeSpan.FromMinutes(5));
                }
                int lineCount = content.Split('\n').Length;
                int charCount = content.Length;
                results.Add(new DataObjects.FileMetadataItem {
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                    LineCount = lineCount,
                    CharCount = charCount
                });
            }
        }
        return Ok(results);
    }

    // POST: api/Data/GetFiles
    // Retrieves a list of files (excluding bin/obj folders and hidden folders).
    [HttpPost($"~/{DataObjects.Endpoints.EndpointFileGpt.GetFiles}")]
    [Authorize(Policy = Policies.AppAdmin)]
    public IActionResult GetFiles([FromBody] DataObjects.FilePathRequest request)
    {
        if (!Directory.Exists(request.Path)) {
            return BadRequest("Directory does not exist");
        }

        var files = Directory.EnumerateFiles(request.Path, "*", SearchOption.AllDirectories)
            .Where(f => {
                var segments = f.Split(Path.DirectorySeparatorChar);
                bool inHiddenFolder = segments.Take(segments.Length - 1)
                    .Any(seg => seg.StartsWith("."));
                if (inHiddenFolder)
                    return false;
                bool binOrObj = segments.Any(seg =>
                    seg.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    seg.Equals("obj", StringComparison.OrdinalIgnoreCase));
                return !binOrObj;
            })
            .Select(f => new DataObjects.FileItem {
                FileName = Path.GetFileName(f),
                FullPath = f
            })
            .ToList();

        return Ok(files);
    }
    #endregion File Operations
}
