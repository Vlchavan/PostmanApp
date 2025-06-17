// Services/DataParsingService.cs
using Newtonsoft.Json.Linq; // Required for JSON parsing
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath; // Required for XML parsing

/// <summary>
/// Service class responsible for parsing JSON and XML content
/// and extracting data based on selected field paths.
/// </summary>
public class DataParsingService
{
    /// <summary>
    /// Discovers all possible fields (paths) within a JSON string.
    /// </summary>
    /// <param name="jsonContent">The JSON content string.</param>
    /// <returns>A list of SelectableField objects.</returns>
    public List<SelectableField> GetJsonFields(string jsonContent)
    {
        var fields = new List<SelectableField>();
        try
        {
            JToken token = JToken.Parse(jsonContent);
            // Start recursive discovery from the root token with an empty path.
            DiscoverJsonFields(token, "", fields);
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            // Handle JSON parsing errors (e.g., malformed JSON)
            System.Console.WriteLine($"JSON parsing error: {ex.Message}");
            // You might want to log this error or return an empty list.
        }
        return fields.OrderBy(f => f.Path).ToList(); // Order for better display
    }

    /// <summary>
    /// Recursively discovers fields within a JSON JToken.
    /// </summary>
    /// <param name="token">The current JToken to inspect.</param>
    /// <param name="currentPath">The current dot-separated path to this token.</param>
    /// <param name="fields">The list to which discovered fields are added.</param>
    private void DiscoverJsonFields(JToken token, string currentPath, List<SelectableField> fields)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                // Iterate over properties of a JSON object
                foreach (JProperty property in token.Children<JProperty>())
                {
                    string newPath = string.IsNullOrEmpty(currentPath) ? property.Name : $"{currentPath}.{property.Name}";
                    // If the value is a primitive type, it's a leaf node we can select
                    if (property.Value.Type != JTokenType.Array && property.Value.Type != JTokenType.Object)
                    {
                        fields.Add(new SelectableField { Path = newPath, DisplayName = property.Name });
                    }
                    // Recursively discover fields within nested objects or arrays
                    DiscoverJsonFields(property.Value, newPath, fields);
                }
                break;
            case JTokenType.Array:
                // For arrays, iterate over its elements.
                // We add a generic array path (e.g., "tags") if it's an array of primitives,
                // or if we want to represent the array itself as a selectable unit.
                if (!string.IsNullOrEmpty(currentPath) && !currentPath.Contains(".")) // Avoid adding sub-arrays again
                {
                    // Add the array itself as a selectable field (e.g., "tags")
                    // The extraction logic will handle the actual array content.
                    if (token.Children().All(c => c.Type != JTokenType.Object && c.Type != JTokenType.Array))
                    {
                        fields.Add(new SelectableField { Path = currentPath, DisplayName = currentPath.Split('.').Last() + " (Array)" });
                    }
                }

                int index = 0;
                foreach (JToken item in token.Children())
                {
                    // For objects within arrays, append an index to the path for discovery.
                    // This creates paths like "users[0].name".
                    string indexedPath = $"{currentPath}[{index}]";
                    DiscoverJsonFields(item, indexedPath, fields);
                    index++;
                }
                break;
            // Primitive types (string, int, bool, etc.) are handled by their parent's discovery
            // and added if they are direct properties of an object.
            case JTokenType.Property:
                // JProperty tokens are handled by their parent JObject
                break;
            default:
                // For other token types (e.g., Null, Undefined), do nothing or log.
                break;
        }
    }

    /// <summary>
    /// Extracts data from a JSON string based on a list of selected paths.
    /// </summary>
    /// <param name="jsonContent">The JSON content string.</param>
    /// <param name="selectedPaths">A list of dot-separated paths to extract.</param>
    /// <returns>A list of lists, where each inner list is a row of extracted values.</returns>
    public List<List<string>> ExtractJsonData(string jsonContent, List<string> selectedPaths)
    {
        var extractedData = new List<List<string>>();
        JToken rootToken = JToken.Parse(jsonContent);

        // Determine if the root is an array of records or a single record object.
        var dataRecords = new List<JToken>();
        if (rootToken.Type == JTokenType.Array)
        {
            // If the root is an array, each element is a potential record.
            dataRecords.AddRange(rootToken.Children());
        }
        else if (rootToken.Type == JTokenType.Object)
        {
            // If the root is a single object, treat it as one record.
            dataRecords.Add(rootToken);
        }
        // If it's a primitive or other type, dataRecords will be empty and no data will be extracted.

        foreach (var record in dataRecords)
        {
            var rowData = new List<string>();
            foreach (var path in selectedPaths)
            {
                JToken selectedToken = record.SelectToken(path); // SelectToken can handle dot notation and array indexing
                if (selectedToken != null)
                {
                    if (selectedToken.Type == JTokenType.Array)
                    {
                        // If the selected token is an array, join its elements for display.
                        rowData.Add($"[{string.Join(", ", selectedToken.Select(t => t.ToString()))}]");
                    }
                    else
                    {
                        rowData.Add(selectedToken.ToString());
                    }
                }
                else
                {
                    rowData.Add(null); // Field not found for this record
                }
            }
            extractedData.Add(rowData);
        }
        return extractedData;
    }


    /// <summary>
    /// Discovers all possible fields (paths) within an XML string using LINQ to XML.
    /// </summary>
    /// <param name="xmlContent">The XML content string.</param>
    /// <returns>A list of SelectableField objects.</returns>
    public List<SelectableField> GetXmlFields(string xmlContent)
    {
        var fields = new List<SelectableField>();
        try
        {
            XDocument doc = XDocument.Parse(xmlContent);
            if (doc.Root != null)
            {
                // Start recursive discovery from the root element.
                DiscoverXmlFields(doc.Root, "", fields);
            }
        }
        catch (System.Xml.XmlException ex)
        {
            // Handle XML parsing errors (e.g., malformed XML)
            System.Console.WriteLine($"XML parsing error: {ex.Message}");
            // You might want to log this error or return an empty list.
        }
        return fields.OrderBy(f => f.Path).ToList(); // Order for better display
    }

    /// <summary>
    /// Recursively discovers fields within an XML XElement.
    /// </summary>
    /// <param name="element">The current XElement to inspect.</param>
    /// <param name="currentPath">The current XPath-like path to this element.</param>
    /// <param name="fields">The list to which discovered fields are added.</param>
    private void DiscoverXmlFields(XElement element, string currentPath, List<SelectableField> fields)
    {
        string newPath = string.IsNullOrEmpty(currentPath) ? element.Name.LocalName : $"{currentPath}/{element.Name.LocalName}";

        // Add attributes of the current element as selectable fields.
        foreach (XAttribute attribute in element.Attributes())
        {
            fields.Add(new SelectableField { Path = $"{newPath}/@{attribute.Name.LocalName}", DisplayName = $"@{attribute.Name.LocalName} ({element.Name.LocalName})" });
        }

        // Add the element's text content itself as a selectable field.
        // Only add if it has direct text content and no child elements (leaf node for text).
        if (element.HasElements == false && !string.IsNullOrWhiteSpace(element.Value))
        {
            fields.Add(new SelectableField { Path = newPath, DisplayName = element.Name.LocalName });
        }

        // Recursively discover fields within child elements.
        foreach (XElement childElement in element.Elements())
        {
            DiscoverXmlFields(childElement, newPath, fields);
        }
    }

    /// <summary>
    /// Extracts data from an XML string based on a list of selected XPath-like paths.
    /// This method assumes a repeating 'record' element (like 'user' in the example XML).
    /// </summary>
    /// <param name="xmlContent">The XML content string.</param>
    /// <param name="selectedPaths">A list of XPath-like paths to extract.</param>
    /// <returns>A list of lists, where each inner list is a row of extracted values.</returns>
    public List<List<string>> ExtractXmlData(string xmlContent, List<string> selectedPaths)
    {
        var extractedData = new List<List<string>>();
        XDocument doc = XDocument.Parse(xmlContent);

        // Define what constitutes a "record" in your XML.
        // For the provided example, each <user> element is a record.
        // You might need to adjust "user" to match your XML structure.
        var recordElements = doc.Descendants("user"); // Assumes 'user' is the repeating record element

        foreach (var record in recordElements)
        {
            var rowData = new List<string>();
            foreach (var path in selectedPaths)
            {
                string value = null;

                if (path.Contains("/@")) // Path points to an attribute
                {
                    // Extract attribute name from path (e.g., "/users/user/@id" -> "id")
                    string attributeName = path.Split("/@").Last();
                    XAttribute attribute = record.Attribute(attributeName); // Try to get attribute from current record
                    if (attribute != null)
                    {
                        value = attribute.Value;
                    }
                    else
                    {
                        // If attribute is not on the record element itself,
                        // try to find it on descendants (less common for simple selection)
                        var fullPathParts = path.Split('/');
                        if (fullPathParts.Length > 1)
                        {
                            XElement targetElement = record;
                            for (int i = 1; i < fullPathParts.Length - 1; i++) // Traverse elements before attribute
                            {
                                targetElement = targetElement?.Element(fullPathParts[i]);
                                if (targetElement == null) break;
                            }
                            attribute = targetElement?.Attribute(attributeName);
                            value = attribute?.Value;
                        }
                    }
                }
                else // Path points to an element's value
                {
                    // This assumes paths like "name" or "details/city" relative to the record element.
                    // If the path is absolute like "/users/user/name", we need to strip the record root.
                    string relativePath = path;
                    if (path.StartsWith(doc.Root.Name.LocalName + "/"))
                    {
                        // Adjust if path includes the root element that is not the record
                        // E.g., "/users/user/name" when record is "user" -> "name"
                        relativePath = path.Substring(path.IndexOf(record.Name.LocalName + "/") + record.Name.LocalName.Length + 1);
                    }
                    else if (path.StartsWith("/"))
                    {
                        // If it's an absolute path, find the element from the document root
                        XElement foundElement = doc.XPathSelectElement(path);
                        if (foundElement != null)
                        {
                            value = foundElement.Value;
                        }
                    }

                    // Find the element relative to the current record.
                    // This simplified approach directly uses Descendants and then filters by local name for now.
                    // For full XPath capabilities, consider `System.Xml.XPath.Extensions.  `.
                    var elements = record.Descendants(relativePath.Split('/').Last());

                    if (elements.Any())
                    {
                        if (elements.Count() > 1 && elements.All(e => e.Parent == record.Element(relativePath.Split('/')[0]) || e.Parent == record))
                        {
                            // If multiple elements with the same name are found directly under the selected path
                            // (e.g., multiple <tag> elements under <tags>), join their values.
                            value = $"[{string.Join(", ", elements.Select(e => e.Value))}]";
                        }
                        else if (relativePath.Contains("/"))
                        {
                            // For nested paths like "details/city", traverse the elements.
                            XElement currentElement = record;
                            foreach (var part in relativePath.Split('/'))
                            {
                                currentElement = currentElement?.Element(part);
                                if (currentElement == null) break;
                            }
                            value = currentElement?.Value;
                        }
                        else
                        {
                            // Single element value
                            value = elements.First().Value;
                        }
                    }
                }
                rowData.Add(value); // Add the extracted value or null
            }
            extractedData.Add(rowData);
        }
        return extractedData;
    }
}
