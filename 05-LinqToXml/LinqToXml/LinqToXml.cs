using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace LinqToXml
{
    public static class LinqToXml
    {
        /// <summary>
        /// Creates hierarchical data grouped by category
        /// </summary>
        /// <param name="xmlRepresentation">Xml representation (refer to CreateHierarchySourceFile.xml in Resources)</param>
        /// <returns>Xml representation (refer to CreateHierarchyResultFile.xml in Resources)</returns>
        public static string CreateHierarchy(string xmlRepresentation)
        {
            var document = XDocument.Parse(xmlRepresentation);
            var newDocument = new XDocument();
            var rootElement = new XElement("Root");
            newDocument.Add(rootElement);

            foreach (var item in document.Element("Root").Elements("Data"))
            {
                var isAdded = false;
                foreach (var groupElement in newDocument.Element("Root").Elements("Group"))
                {
                    if (groupElement.Attributes("ID").Any(attributeId => attributeId.Value == item.Element("Category").Value))
                    {
                        var newElement = new XElement("Data",
                        new XElement("Quantity", item.Element("Quantity").Value),
                        new XElement("Price", string.Format("{0:0.00}", item.Element("Price").Value)));
                        groupElement.Add(newElement);
                        isAdded = true;
                        break;
                    }
                }
                if (isAdded == false)
                {
                    var newElement = new XElement("Group", new XAttribute("ID", item.Element("Category").Value),
                    new XElement("Data",
                    new XElement("Quantity", item.Element("Quantity").Value),
                    new XElement("Price",
                    string.Format("{0:0.00}", item.Element("Price").Value))));
                    newDocument.Element("Root").Add(newElement);
                }
            }
            return newDocument.ToString();
        }

        /// <summary>
        /// Get list of orders numbers (where shipping state is NY) from xml representation
        /// </summary>
        /// <param name="xmlRepresentation">Orders xml representation (refer to PurchaseOrdersSourceFile.xml in Resources)</param>
        /// <returns>Concatenated orders numbers</returns>
        /// <example>
        /// 99301,99189,99110
        /// </example>
        public static string GetPurchaseOrders(string xmlRepresentation)
        {
            var document = XDocument.Parse(xmlRepresentation);
            var ns = document.Root.GetNamespaceOfPrefix(
            document.Root.Attributes().First(a => a.IsNamespaceDeclaration).Name.LocalName);

            var result = new List<string>();

            foreach (var order in document.Element(ns + "PurchaseOrders").Elements(ns + "PurchaseOrder"))
            {
                foreach (var address in order.Elements(ns + "Address"))
                {
                    if (address.Attribute(ns + "Type").Value == "Shipping" && address.Element(ns + "State").Value == "NY")
                    {
                        result.Add(order.Attribute(ns + "PurchaseOrderNumber").Value);
                    }
                }
            }
            return string.Join(",", result);
        }

        /// <summary>
        /// Reads csv representation and creates appropriate xml representation
        /// </summary>
        /// <param name="customers">Csv customers representation (refer to XmlFromCsvSourceFile.csv in Resources)</param>
        /// <returns>Xml customers representation (refer to XmlFromCsvResultFile.xml in Resources)</returns>
        public static string ReadCustomersFromCsv(string customers)
        {
            var customer = new XElement("Root");
            foreach (var fields in Regex.Split(customers, "\r\n").Select(c => c.Split(',')))
            {
                customer.Add(new XElement("Customer",
                new XAttribute("CustomerID", fields[0]),
                new XElement("CompanyName", fields[1]),
                new XElement("ContactName", fields[2]),
                new XElement("ContactTitle", fields[3]),
                new XElement("Phone", fields[4]),
                new XElement("FullAddress",
                new XElement("Address", fields[5]),
                new XElement("City", fields[6]),
                new XElement("Region", fields[7]),
                new XElement("PostalCode", fields[8]),
                new XElement("Country", fields[9]))));
            }
            return customer.ToString();
        }

        /// <summary>
        /// Gets recursive concatenation of elements
        /// </summary>
        /// <param name="xmlRepresentation">Xml representation of document with Sentence, Word and Punctuation elements. (refer to ConcatenationStringSource.xml in Resources)</param>
        /// <returns>Concatenation of all this element values.</returns>
        public static string GetConcatenationString(string xmlRepresentation)
        {
            return XDocument.Parse(xmlRepresentation).Element("Document").Elements("Sentence")
            .Aggregate(string.Empty, (current, element) => current + element.Value);
        }

        /// <summary>
        /// Replaces all "customer" elements with "contact" elements with the same childs
        /// </summary>
        /// <param name="xmlRepresentation">Xml representation with customers (refer to ReplaceCustomersWithContactsSource.xml in Resources)</param>
        /// <returns>Xml representation with contacts (refer to ReplaceCustomersWithContactsResult.xml in Resources)</returns>
        public static string ReplaceAllCustomersWithContacts(string xmlRepresentation)
        {
            xmlRepresentation = xmlRepresentation.Replace("<customer>", "<contact>");
            xmlRepresentation = xmlRepresentation.Replace("</customer>", "</contact>");
            xmlRepresentation = xmlRepresentation.Replace("<customer/>", "<contact/>");
            return xmlRepresentation;
            var document = XDocument.Parse(xmlRepresentation);
            var customers = document.Element("Document").Elements("customer");
            foreach (var customer in customers)
            {
                var element = new XElement(customer) { Name = "contact" };
                customer.AddBeforeSelf(element);
            }
            customers.Remove();
            return document.ToString();
        }

        /// <summary>
        /// Finds all ids for channels with 2 or more subscribers and mark the "DELETE" comment
        /// </summary>
        /// <param name="xmlRepresentation">Xml representation with channels (refer to FindAllChannelsIdsSource.xml in Resources)</param>
        /// <returns>Sequence of channels ids</returns>
        public static IEnumerable<int> FindChannelsIds(string xmlRepresentation)
        {
            return XDocument.Parse(xmlRepresentation).Element("service").Elements("channel").

            Where(channel => channel.Elements("subscriber").Count() >= 2 &&
            channel.DescendantNodes().OfType<XComment>()
            .Any(comment => comment.Value == "DELETE"))
            .Select(channel => Convert.ToInt32(channel.Attribute("id").Value));
        }

        /// <summary>
        /// Sort customers in docement by Country and City
        /// </summary>
        /// <param name="xmlRepresentation">Customers xml representation (refer to GeneralCustomersSourceFile.xml in Resources)</param>
        /// <returns>Sorted customers representation (refer to GeneralCustomersResultFile.xml in Resources)</returns>
        public static string SortCustomers(string xmlRepresentation)
        {
            XElement root = XElement.Parse(xmlRepresentation);
            var orderedtabs = root.Elements("Customers")
            .OrderBy(x => x.Element("FullAddress").Element("Country").Value)
            .ThenBy(x => x.Element("FullAddress").Element("City").Value)
            .ToArray();
            root.RemoveAll();
            foreach (var tab in orderedtabs)
                root.Add(tab);
            return root.ToString();
        }

        /// <summary>
        /// Gets XElement flatten string representation to save memory
        /// </summary>
        /// <param name="xmlRepresentation">XElement object</param>
        /// <returns>Flatten string representation</returns>
        /// <example>
        ///     <root><element>something</element></root>
        /// </example>
        public static string GetFlattenString(XElement xmlRepresentation)
        {
            var str = xmlRepresentation.ToString();
            var escapeChars = new[] { ' ', '\n', '\t', '\r', '\f', '\v', '\\' };
            var toReturn = string.Empty;

            for (int i = 0; i < str.Length; i++)
            {
                toReturn += str[i];
                if (str[i] != '>') continue;
                while (true)
                {
                    i++;
                    if (i >= str.Length) break;
                    if (escapeChars.Contains(str[i])) continue;
                    i--;
                    break;
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Gets total value of orders by calculating products value
        /// </summary>
        /// <param name="xmlRepresentation">Orders and products xml representation (refer to GeneralOrdersFileSource.xml in Resources)</param>
        /// <returns>Total purchase value</returns>
        public static int GetOrdersValue(string xmlRepresentation)
        {
            var document = XDocument.Parse(xmlRepresentation);
            int totalvalue = 0;

            var root = document.Element("Root");
            var orders = root.Element("Orders").Elements("Order");
            var products = root.Element("products");

            foreach (var order in orders)
            {
                foreach (var product in products.Descendants())
                {
                    if (product.Attribute("Id").Value == order.Element("product").Value)
                    {
                        totalvalue += Convert.ToInt32(product.Attribute("Value").Value);
                        break;
                    }
                }
            }

            return totalvalue;
        }
    }
}
