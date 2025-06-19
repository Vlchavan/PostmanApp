using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json; // Make sure to install the Newtonsoft.Json NuGet package.
using Newtonsoft.Json.Linq; // Used for dynamic JSON parsing to DataTable.

namespace Postman.BusinessLogicDynamicDataConverter
{
    /// <summary>
    /// Provides static methods to convert JSON and XML strings into List<T> or DataTable objects.
    /// This class is designed to be compiled into a dynamic library (DLL) for reuse.
    /// </summary>
    public static class DataConverter
    {
        #region JSON Conversion Methods

        /// <summary>
        /// Converts a JSON string representing an array of objects into a List of specified type T.
        /// This method requires that the JSON structure matches the properties of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list.</typeparam>
        /// <param name="jsonString">The JSON string to convert.</param>
        /// <returns>A List of objects of type T, or an empty list if conversion fails or JSON is empty.</returns>
        /// <example>
        /// public class User { public int Id { get; set; } public string Name { get; set; } }
        /// string json = "[{\"Id\": 1, \"Name\": \"Alice\"}, {\"Id\": 2, \"Name\": \"Bob\"}]";
        /// List<User> users = DataConverter.JsonToList<User>(json);
        /// </example>
        public static List<T> JsonToList<T>(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Console.WriteLine("Warning: JSON string is null or empty for JsonToList<T>.");
                return new List<T>();
            }

            try
            {
                // Deserialize the JSON string directly into a List of type T.
                // Newtonsoft.Json handles mapping JSON properties to T's properties.
                return JsonConvert.DeserializeObject<List<T>>(jsonString) ?? new List<T>();
            }
            catch (JsonSerializationException ex)
            {
                Console.WriteLine($"Error deserializing JSON to List<T>: {ex.Message}");
                Console.WriteLine($"JSON String: {jsonString}");
                return new List<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during JsonToList<T>: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Converts a JSON string into a DataTable.
        /// This method dynamically creates columns based on the first JSON object encountered.
        /// It assumes the JSON represents an array of objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to convert.</param>
        /// <returns>A DataTable containing the JSON data, or an empty DataTable if conversion fails or JSON is empty.</returns>
        /// <example>
        /// string json = "[{\"Id\": 1, \"Name\": \"Alice\", \"Age\": 30}, {\"Id\": 2, \"Name\": \"Bob\", \"Age\": 24}]";
        /// DataTable dt = DataConverter.JsonToDataTable(json);
        /// // You can then bind dt to a DataGridView or process it further.
        /// </example>
        public static DataTable JsonToDataTable(string jsonString)
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Console.WriteLine("Warning: JSON string is null or empty for JsonToDataTable.");
                return dt;
            }

            try
            {
                // Parse the JSON string into a JArray to easily iterate over objects.
                JArray jsonArray = JArray.Parse(jsonString);

                if (!jsonArray.HasValues)
                {
                    Console.WriteLine("Info: JSON array has no values. Returning empty DataTable.");
                    return dt;
                }

                // Create columns from the properties of the first JSON object.
                // This assumes all objects in the array have a consistent schema.
                foreach (JProperty property in jsonArray.First.Children<JProperty>())
                {
                    dt.Columns.Add(property.Name, typeof(string)); // Default to string type for flexibility
                                                                   // You could try to infer types here, but it adds complexity.
                }

                // Populate the DataTable rows.
                foreach (JObject jsonObject in jsonArray.Children<JObject>())
                {
                    DataRow row = dt.NewRow();
                    foreach (JProperty property in jsonObject.Children<JProperty>())
                    {
                        if (dt.Columns.Contains(property.Name))
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                    }
                    dt.Rows.Add(row);
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Error parsing JSON to DataTable: {ex.Message}");
                Console.WriteLine($"JSON String: {jsonString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during JsonToDataTable: {ex.Message}");
            }

            return dt;
        }

        #endregion

        #region XML Conversion Methods

        /// <summary>
        /// Converts an XML string into a List of specified type T.
        /// This method requires a specific 'itemElementName' to identify individual objects in the XML.
        /// The XML structure should be simple, with properties directly mapping to element/attribute names.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list.</typeparam>
        /// <param name="xmlString">The XML string to convert.</param>
        /// <param name="itemElementName">The name of the XML element that represents an individual item (e.g., "User").</param>
        /// <returns>A List of objects of type T, or an empty list if conversion fails or XML is empty.</returns>
        /// <example>
        /// public class Product { public int Id { get; set; } public string Name { get; set; } }
        /// string xml = "<Products><Product><Id>101</Id><Name>Laptop</Name></Product><Product><Id>102</Id><Name>Mouse</Name></Product></Products>";
        /// List<Product> products = DataConverter.XmlToList<Product>(xml, "Product");
        /// </example>
        public static List<T> XmlToList<T>(string xmlString, string itemElementName) where T : new()
        {
            List<T> list = new List<T>();
            if (string.IsNullOrWhiteSpace(xmlString) || string.IsNullOrWhiteSpace(itemElementName))
            {
                Console.WriteLine("Warning: XML string or item element name is null/empty for XmlToList<T>.");
                return list;
            }

            try
            {
                XDocument doc = XDocument.Parse(xmlString);

                // Iterate through elements that match the specified itemElementName.
                foreach (XElement itemElement in doc.Descendants(itemElementName))
                {
                    T item = new T();
                    // Use reflection to set properties of T from XML elements/attributes.
                    // This is a basic implementation; more robust XML mapping might use attributes
                    // or a dedicated XML serialization library.
                    foreach (var prop in typeof(T).GetProperties())
                    {
                        XElement propElement = itemElement.Element(prop.Name);
                        if (propElement != null)
                        {
                            try
                            {
                                // Convert the element's value to the property's type.
                                prop.SetValue(item, Convert.ChangeType(propElement.Value, prop.PropertyType), null);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Could not convert XML element '{prop.Name}' value '{propElement.Value}' to type '{prop.PropertyType.Name}': {ex.Message}");
                            }
                        }
                        else
                        {
                            XAttribute propAttribute = itemElement.Attribute(prop.Name);
                            if (propAttribute != null)
                            {
                                try
                                {
                                    // Convert the attribute's value to the property's type.
                                    prop.SetValue(item, Convert.ChangeType(propAttribute.Value, prop.PropertyType), null);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Warning: Could not convert XML attribute '{prop.Name}' value '{propAttribute.Value}' to type '{prop.PropertyType.Name}': {ex.Message}");
                                }
                            }
                        }
                    }
                    list.Add(item);
                }
            }
            catch (System.Xml.XmlException ex)
            {
                Console.WriteLine($"Error parsing XML to List<T>: {ex.Message}");
                Console.WriteLine($"XML String: {xmlString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during XmlToList<T>: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Converts an XML string into a DataTable.
        /// This method requires a specific 'itemElementName' to identify individual rows in the XML.
        /// It creates columns based on the child elements and attributes of the first 'itemElementName' element.
        /// </summary>
        /// <param name="xmlString">The XML string to convert.</param>
        /// <param name="itemElementName">The name of the XML element that represents an individual row (e.g., "Row", "Item", "Record").</param>
        /// <returns>A DataTable containing the XML data, or an empty DataTable if conversion fails or XML is empty.</returns>
        /// <example>
        /// string xml = "<Root><Record Id='1' Name='Anna'><City>New York</City></Record><Record Id='2' Name='John'><City>London</City></Record></Root>";
        /// DataTable dt = DataConverter.XmlToDataTable(xml, "Record");
        /// // You can then bind dt to a DataGridView or process it further.
        /// </example>
        public static DataTable XmlToDataTable(string xmlString, string itemElementName)
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrWhiteSpace(xmlString) || string.IsNullOrWhiteSpace(itemElementName))
            {
                Console.WriteLine("Warning: XML string or item element name is null/empty for XmlToDataTable.");
                return dt;
            }

            try
            {
                XDocument doc = XDocument.Parse(xmlString);

                // Find all elements representing a single item (row)
                var itemElements = doc.Descendants(itemElementName);

                if (!itemElements.Any())
                {
                    Console.WriteLine($"Info: No XML elements found with name '{itemElementName}'. Returning empty DataTable.");
                    return dt;
                }

                // Create columns from the attributes and child elements of the first item
                XElement firstItem = itemElements.First();

                // Add columns for attributes
                foreach (XAttribute attr in firstItem.Attributes())
                {
                    if (!dt.Columns.Contains(attr.Name.LocalName))
                    {
                        dt.Columns.Add(attr.Name.LocalName, typeof(string));
                    }
                }

                // Add columns for child elements
                foreach (XElement childElement in firstItem.Elements())
                {
                    if (!dt.Columns.Contains(childElement.Name.LocalName))
                    {
                        dt.Columns.Add(childElement.Name.LocalName, typeof(string));
                    }
                }

                // Populate the DataTable rows
                foreach (XElement item in itemElements)
                {
                    DataRow row = dt.NewRow();

                    // Fill row with attribute values
                    foreach (XAttribute attr in item.Attributes())
                    {
                        if (dt.Columns.Contains(attr.Name.LocalName))
                        {
                            row[attr.Name.LocalName] = attr.Value;
                        }
                    }

                    // Fill row with child element values
                    foreach (XElement childElement in item.Elements())
                    {
                        if (dt.Columns.Contains(childElement.Name.LocalName))
                        {
                            row[childElement.Name.LocalName] = childElement.Value;
                        }
                    }
                    dt.Rows.Add(row);
                }
            }
            catch (System.Xml.XmlException ex)
            {
                Console.WriteLine($"Error parsing XML to DataTable: {ex.Message}");
                Console.WriteLine($"XML String: {xmlString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during XmlToDataTable: {ex.Message}");
            }

            return dt;
        }

        #endregion
    }
}
