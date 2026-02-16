    namespace JobResearchAgent.Services;
    
    public class FileSanitizer
    {
        public static string Sanitize(string input)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                input = input.Replace(c, '_');

            return input.Replace(" ", "");
        }
    }