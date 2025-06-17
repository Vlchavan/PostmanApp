// Models/ExtractionViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Required for [Required]

/// <summary>
/// ViewModel for handling the selected fields and displaying the extracted data.
/// </summary>
public class ExtractionViewModel
{
    /// <summary>
    /// The type of the original file ("json" or "xml").
    /// </summary>
    public string FileType { get; set; }

    /// <summary>
    /// The original raw content of the file, needed for re-parsing with selected fields.
    /// </summary>
    public string OriginalContent { get; set; }

    /// <summary>
    /// A list of paths to the fields selected by the user for extraction.
    /// </summary>
    [Required(ErrorMessage = "Please select at least one field.")]
    public List<string> SelectedFieldPaths { get; set; } = new List<string>();

    /// <summary>
    /// The final extracted data, represented as a list of rows (lists of strings).
    /// Each inner list corresponds to a record, and its elements correspond to the values
    /// of the selected fields in the order they were selected.
    /// </summary>
    public List<List<string>> ExtractedData { get; set; } = new List<List<string>>();
}
