namespace FreeLLM;

// Use this file as a place to put any application-specific data objects.

public partial class DataObjects
{
    public static partial class Endpoints
    {
        public static class EndpointFileGpt
        {
            public const string GetFileContents = "api/Data/GetFileContents";
            public const string GetFileMetadata = "api/Data/GetFileMetadata";
            public const string GetFiles = "api/Data/GetFiles";
        }
    }

    public class FileContentItem
    {
        public string Content { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }

    // Existing FileItem used in API responses.
    public class FileItem
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }

    // NEW: Metadata for files
    public class FileMetadataItem
    {
        public int CharCount { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public int LineCount { get; set; }
    }


    public record FilePathRequest(string Path);
    public record FileContentRequest(List<string> FilePaths);
}