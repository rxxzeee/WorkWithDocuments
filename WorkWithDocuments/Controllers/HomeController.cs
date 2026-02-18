//using Microsoft.AspNetCore.Mvc;
//using WorkWithDocuments.Data;
//using WorkWithDocuments.Models;
//using WorkWithDocuments.Services.Interfaces;

//public class HomeController : Controller
//{
//    private readonly IFileProcessingService _fileService;
//    private readonly AppDbContext _context;

//    // Впровадження залежностей через конструктор
//    public HomeController(IFileProcessingService fileService, AppDbContext context)
//    {
//        _fileService = fileService;
//        _context = context;
//    }

//    public IActionResult Index()
//    {
//        return View();
//    }

//    [HttpPost]
//    public async Task<IActionResult> Process(IFormFile file, string keywords)
//    {
//        if (file == null || file.Length == 0)
//        {
//            ModelState.AddModelError("", "Please select a file.");
//            return View("Index");
//        }

//        if (string.IsNullOrEmpty(keywords))
//        {
//            ModelState.AddModelError("", "Please enter keywords.");
//            return View("Index");
//        }

//        var extension = Path.GetExtension(file.FileName);
//        if (extension != ".pdf" && extension != ".html")
//        {
//            ModelState.AddModelError("", "Only PDF and HTML files are allowed.");
//            return View("Index");
//        }

//        try
//        {
//            // 1. Work through the file service to process the file V.S
//            byte[] processedData;
//            using (var stream = file.OpenReadStream())
//            {
//                processedData = _fileService.ProcessFile(stream, keywords, extension);
//            }

//            // 2. Save in DB V.S
//            var dbRecord = new ProcessedFile
//            {
//                Name = "Processed_" + file.FileName,
//                Data = processedData,
//                CreatedAt = DateTime.Now
//            };

//            _context.ProcessedFiles.Add(dbRecord);
//            await _context.SaveChangesAsync();

//            // 3. Return the processed file to the user V.S
//            return File(processedData, "application/octet-stream", dbRecord.Name);
//        }
//        catch (Exception ex)
//        {
//            // Log the exception (not shown here for brevity) V.S
//            ModelState.AddModelError("", $"Error processing file: {ex.Message}");
//            return View("Index");
//        }
//    }
//}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Потрібно для асинхронних методів EF
using WorkWithDocuments.Data;
using WorkWithDocuments.Models;
using WorkWithDocuments.Services.Interfaces;

public class HomeController : Controller
{
    private readonly IFileProcessingService _fileService;
    private readonly AppDbContext _context;

    public HomeController(IFileProcessingService fileService, AppDbContext context)
    {
        _fileService = fileService;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Додаємо ліміт на розмір файлу (наприклад, 50 МБ), щоб не забити пам'ять
    [HttpPost]
    [RequestSizeLimit(52428800)]
    public async Task<IActionResult> Process(IFormFile file, string keywords, CancellationToken cancellationToken)
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
            byte[] processedData;

            // Використовуємо OpenReadStream без повного копіювання в MemoryStream до обробки
            using (var stream = file.OpenReadStream())
            {
                // Передаємо stream напряму в сервіс
                // В реальному проекті тут краще також передати cancellationToken, 
                // але Spire.PDF і HtmlAgilityPack не мають повноцінної підтримки async/cancel
                processedData = _fileService.ProcessFile(stream, keywords, extension);
            }

            // Перевіряємо, чи користувач не скасував запит, поки ми обробляли файл
            cancellationToken.ThrowIfCancellationRequested();

            var dbRecord = new ProcessedFile
            {
                Name = "Processed_" + file.FileName,
                Data = processedData,
                CreatedAt = DateTime.Now
            };

            _context.ProcessedFiles.Add(dbRecord);

            // Зберігаємо асинхронно з підтримкою скасування
            await _context.SaveChangesAsync(cancellationToken);

            // Повертаємо файл
            return File(processedData, "application/octet-stream", dbRecord.Name);
        }
        catch (OperationCanceledException)
        {
            // Якщо запит скасовано - просто нічого не робимо (сервер не навантажується)
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error: {ex.Message}");
            return View("Index");
        }
    }
}