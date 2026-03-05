using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;

namespace QuickMetadata
{
    public static class Extractor
    {
        public static Dictionary<string, string> ExtractFields(string filePath, Dictionary<string, string> tagNames)
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            foreach (var directory in directories)
                foreach (var tag in directory.Tags)
                    if (tagNames.ContainsKey(tag.Name))
                        tagNames[tag.Name] = tag.Description ?? "";

            var xmpDirectory = directories.OfType<XmpDirectory>().FirstOrDefault();
            if (xmpDirectory?.GetXmpProperties() is { } xmpProps)
                foreach (var xmpTag in xmpProps)
                    if (tagNames.ContainsKey(xmpTag.Key))
                        tagNames[xmpTag.Key] = xmpTag.Value ?? "";

            return tagNames;
        }


        public static List<Dictionary<string, string>> ExtractFromFiles(IEnumerable<string> filePaths, IEnumerable<string> tagNames)
        {
            var results = new List<Dictionary<string, string>>();

            foreach (var filePath in filePaths)
            {
                var tagDict = tagNames.ToDictionary(t => t, t => "");
                results.Add(ExtractFields(filePath, tagDict));
            }

            return results;
        }
    }
}
