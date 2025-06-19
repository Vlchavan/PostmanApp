// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // Required for Any()

public class HomeController : Controller
{
    private readonly DataParsingService _dataParsingService;

    /// <summary>
    /// Constructor for HomeController, injecting DataParsingService.
    /// </summary>
    /// <param name="dataParsingService">The service for parsing JSON/XML data.</param>
    public HomeController(DataParsingService dataParsingService)
    {
        _dataParsingService = dataParsingService;
    }

    /// <summary>
    /// Displays the initial file upload form.
    /// GET: /Home/Index
    /// </summary>
    public IActionResult Index()
    {
        // Return an empty UploadViewModel to populate the form.
        return View(new UploadViewModel());
    }
    public IActionResult Conversion()
    {
        // Return an empty UploadViewModel to populate the form.
        return View(new UploadViewModel());
    }
    /// <summary>
    /// Handles the uploaded file, parses it, and discovers available fields.
    /// POST: /Home/UploadFile
    /// </summary>
    /// <param name="model">The UploadViewModel containing the uploaded file.</param>
    /// <returns>A view to select fields or an error view if upload fails.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken] // Protects against Cross-Site Request Forgery attacks
    public async Task<IActionResult> UploadFile(UploadViewModel model)
    {
        // Check if a file was actually uploaded and if model state is valid.
        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError("File", "Please select a file to upload.");
            return View("Index", model); // Return to Index view with error
        }

        // Determine file type based on content type for initial parsing.
        model.FileType = model.File.ContentType.Contains("json") ? "json" :
                         model.File.ContentType.Contains("xml") ? "xml" :
                         Path.GetExtension(model.File.FileName).ToLower() == ".json" ? "json" :
                         Path.GetExtension(model.File.FileName).ToLower() == ".xml" ? "xml" :
                         null; // Fallback if content type isn't definitive

        if (string.IsNullOrEmpty(model.FileType))
        {
            ModelState.AddModelError("File", "Unsupported file type. Please upload a JSON (.json) or XML (.xml) file.");
            return View("Index", model);
        }

        string fileContent;
        // Read the file content into a string.
        using (var reader = new StreamReader(model.File.OpenReadStream(), Encoding.UTF8))
        {
            fileContent = await reader.ReadToEndAsync();
        }

        model.ParsedContent = fileContent; // Store the raw content for later extraction

        // Discover fields based on the detected file type.
        if (model.FileType == "json")
        {
            model.AvailableFields = _dataParsingService.GetJsonFields(fileContent);
        }
        else // XML
        {
            model.AvailableFields = _dataParsingService.GetXmlFields(fileContent);
        }

        // If no fields are found, it indicates an issue with the file content or type.
        if (!model.AvailableFields.Any())
        {
            ModelState.AddModelError("", "No fields could be extracted from the file. Please ensure it's valid JSON or XML.");
            return View("Index", model); // Go back to upload with an error message
        }

        // Pass the model to the SelectFields view for user selection.
        return View("SelectFields", model);
    }

    /// <summary>
    /// Handles the selection of fields and performs the data extraction.
    /// POST: /Home/ExtractData
    /// </summary>
    /// <param name="model">The ExtractionViewModel containing selected field paths and original content.</param>
    /// <returns>A view displaying the extracted data or an error view.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExtractData(ExtractionViewModel model)
    {
        // Check if any fields were selected.
        if (model.SelectedFieldPaths == null || !model.SelectedFieldPaths.Any())
        {
            ModelState.AddModelError("SelectedFieldPaths", "Please select at least one field to extract data.");
            // To properly re-render SelectFields, you would need to reconstruct UploadViewModel
            // from OriginalContent and FileType. For simplicity here, we return to Index.
            return View("Index");
        }

        // Perform data extraction based on file type and selected paths.
        if (model.FileType == "json")
        {
            model.ExtractedData = _dataParsingService.ExtractJsonData(model.OriginalContent, model.SelectedFieldPaths);
        }
        else if (model.FileType == "xml")
        {
            model.ExtractedData = _dataParsingService.ExtractXmlData(model.OriginalContent, model.SelectedFieldPaths);
        }
        else
        {
            // This case should ideally not happen if previous steps validate FileType correctly.
            ModelState.AddModelError("FileType", "Unsupported file type for data extraction.");
            return View("Index");
        }

        // Display the extracted data in the ExtractedData view.
        return View("ExtractedData", model);
    }
}
