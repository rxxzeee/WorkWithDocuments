namespace WorkWithDocuments.Services.Interfaces
{
    public interface IFileProcessingService
    {
        byte[] ProcessFile(Stream inputStream, string keywords, string fileExtension);
    }
}
