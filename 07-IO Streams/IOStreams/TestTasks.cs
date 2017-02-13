using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace IOStreams
{

	public static class TestTasks
	{
        /// <summary>
        /// Parses Resourses\Planets.xlsx file and returns the planet data: 
        ///   Jupiter     69911.00
        ///   Saturn      58232.00
        ///   Uranus      25362.00
        ///    ...
        /// See Resourses\Planets.xlsx for details
        /// </summary>
        /// <param name="xlsxFileName">source file name</param>
        /// <returns>sequence of PlanetInfo</returns>
        public static IEnumerable<PlanetInfo> ReadPlanetInfoFromXlsx(string xlsxFileName)
        {
        const string stringsPath = "/xl/sharedStrings.xml";
        const string sheetPath = "/xl/worksheets/sheet1.xml";
        using (var package = Package.Open(xlsxFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var xDoc = LoadXDocument(package, stringsPath);
                XNamespace ns = GetNamespace(xDoc);
                var strings = xDoc.Root.Descendants(ns + "t").Select(x => x.Value).ToList();
                xDoc = LoadXDocument(package, sheetPath);
                ns = GetNamespace(xDoc);
                return xDoc.Descendants(ns + "row").Skip(1).Select(x =>
                new PlanetInfo
                    {
                    Name = strings[(int)x.Descendants(ns + "v").First()],
                    MeanRadius = (double)x.Descendants(ns + "v").Skip(1).First()
                    });
             }
        }
 
		private static XDocument LoadXDocument(Package package, string path)
		{
			using (var stream = GetPartStream(package, path))
			{
				var xDocument = XDocument.Load(stream);
				return xDocument;
			}
		}

		private static XNamespace GetNamespace(XDocument doc)
		{
			return doc.Root.Name.Namespace;
		}

		private static Stream GetPartStream(Package package, string path)
		{
			return package.GetPart(new Uri(path, UriKind.Relative)).GetStream();
		}


		/// <summary>
		/// Calculates hash of stream using specifued algorithm
		/// </summary>
		/// <param name="stream">source stream</param>
		/// <param name="hashAlgorithmName">hash algorithm ("MD5","SHA1","SHA256" and other supported by .NET)</param>
		/// <returns></returns>
		public static string CalculateHash(this Stream stream, string hashAlgorithmName) { 
			using (var hashAgorithm = HashAlgorithm.Create(hashAlgorithmName))
			{
				if (hashAgorithm == null)
					throw new ArgumentException(hashAlgorithmName);

				var buffer = hashAgorithm.ComputeHash(stream);
				return string.Join(string.Empty, buffer.Select(x=> x.ToString("X2")));
			}
}


        /// <summary>
/// Returns decompressed strem from file. 
/// </summary>
/// <param name="fileName">source file</param>
/// <param name="method">method used for compression (none, deflate, gzip)</param>
/// <returns>output stream</returns>
        public static Stream DecompressStream(string fileName, DecompressionMethods method) { 
		var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		switch (method)
		{
			case DecompressionMethods.None:
				return fileStream;
			case DecompressionMethods.Deflate:
				return new DeflateStream(fileStream, CompressionMode.Decompress);
			case DecompressionMethods.GZip:
				return new GZipStream(fileStream, CompressionMode.Decompress);
			default:
				throw new ArgumentException("method");
		}
}


        /// <summary>
/// Reads file content econded with non Unicode encoding
/// </summary>
/// <param name="fileName">source file name</param>
/// <param name="encoding">encoding name</param>
/// <returns>Unicoded file content</returns>
        public static string ReadEncodedText(string fileName, string encoding)
        {
            var thisEncoding = Encoding.GetEncoding(encoding);
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (var streamReader = new StreamReader(fileStream, thisEncoding))
            {
                return streamReader.ReadToEnd();
            }
        }
        }


	public class PlanetInfo : IEquatable<PlanetInfo>
	{
		public string Name { get; set; }
		public double MeanRadius { get; set; }

		public override string ToString()
		{
			return string.Format("{0} {1}", Name, MeanRadius);
		}

		public bool Equals(PlanetInfo other)
		{
			return Name.Equals(other.Name)
				&& MeanRadius.Equals(other.MeanRadius);
		}
	}



}
