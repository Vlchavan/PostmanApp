// Models/UploadViewModel.cs
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Required for [Required]

/// <summary>
/// ViewModel for handling file uploads and displaying discovered fields.
/// </summary>
public class UploadViewModel
{
    /// <summary>
    /// The uploaded file (JSON or XML).
    /// </summary>
    [Required(ErrorMessage = "Please select a file.")]
    public IFormFile File { get; set; }

    /// <summary>
    /// The type of the uploaded file ("json" or "xml").
    /// Determined from ContentType during upload.
    /// </summary>
    public string FileType { get; set; }

    /// <summary>
    /// The raw content of the uploaded file, stored as a string.
    /// This is passed to the next stage for re-parsing based on selected fields.
    /// Html.Raw() is used in the view for safety.
    /// </summary>
    public string ParsedContent { get; set; }

    /// <summary>
    /// A list of all discoverable fields within the uploaded file.
    /// These are presented to the user for selection.
    /// </summary>
    public List<SelectableField> AvailableFields { get; set; } = new List<SelectableField>();
}
