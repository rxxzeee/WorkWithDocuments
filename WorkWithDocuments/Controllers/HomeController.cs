using Microsoft.AspNetCore.Mvc;
using WorkWithDocuments.Data;
using WorkWithDocuments.Models;
using WorkWithDocuments.Services.Interfaces;

public class HomeController : Controller
{
    private readonly IFileProcessingService _fileService;
    private readonly AppDbContext _context;

    // Впровадження залежностей через конструктор
    public HomeController(IFileProcessingService fileService, AppDbContext context)
    {
        _fileService = fileService;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Process(IFormFile file, string keywords)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file.");
            return View("Index");
        }

        if (string.IsNullOrEmpty(keywords))
        {
            ModelState.AddModelError("", "Please enter keywords.");
            return View("Index");
        }

        var extension = Path.GetExtension(file.FileName);
        if (extension != ".pdf" && extension != ".html")
        {
            ModelState.AddModelError("", "Only PDF and HTML files are allowed.");
            return View("Index");
        }

        try
        {
            // 1. Work through the file service to process the file V.S
            byte[] processedData;
            using (var stream = file.OpenReadStream())
            {
                processedData = _fileService.ProcessFile(stream, keywords, extension);
            }

            // 2. Save in DB V.S
            var dbRecord = new ProcessedFile
            {
                Name = "Processed_" + file.FileName,
                Data = processedData,
                CreatedAt = DateTime.Now
            };

            _context.ProcessedFiles.Add(dbRecord);
            await _context.SaveChangesAsync();

            // 3. Return the processed file to the user V.S
            return File(processedData, "application/octet-stream", dbRecord.Name);
        }
        catch (Exception ex)
        {
            // Log the exception (not shown here for brevity) V.S
            ModelState.AddModelError("", $"Error processing file: {ex.Message}");
            return View("Index");
        }
    }
}
