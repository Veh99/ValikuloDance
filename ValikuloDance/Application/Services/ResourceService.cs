using System.Reflection;

namespace ValikuloDance.Application.Services
{
    public class ResourceService
    {
        public string GetResourceContent(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Resource '{resourceName}' not found");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public string GetImageAsBase64(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Resource '{resourceName}' not found");

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            return $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";
        }
    }
}
