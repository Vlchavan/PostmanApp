// Models/SelectableField.cs
using System.Collections.Generic;

/// <summary>
/// Represents a field that can be selected from the parsed JSON or XML data.
/// </summary>
public class SelectableField
{
    /// <summary>
    /// The path to the field (e.g., "users[0].name" for JSON, "/users/user/name" for XML).
    /// This is used internally for extraction.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// A human-readable name for the field, derived from its path or key.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Indicates whether this field has been selected by the user.
    /// (Primarily used when the model is sent from the UI back to the server, though not explicitly
    /// bound by the current checkbox logic which uses a separate list for selected paths).
    /// </summary>
    public bool IsSelected { get; set; }
}
