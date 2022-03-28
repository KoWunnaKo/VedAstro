﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure.Storage.Blobs;
using Genso.Astrology.Library;
using Genso.Astrology.Library.Compatibility;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace API
{
    /// <summary>
    /// A collection of general tools used by API
    /// </summary>
    public static class Tools
    {

        /// <summary>
        /// Overwrites new XML data to a blob file
        /// </summary>
        public static async Task OverwriteBlobData(BlobClient blobClient, XDocument newData)
        {
            //convert xml data to string
            var dataString = newData.ToString();

            //convert xml string to stream
            var dataStream = GenerateStreamFromString(dataString);

            //upload stream to blob
            await blobClient.UploadAsync(dataStream, overwrite: true);
        }

        /// <summary>
        /// Adds an XML element to XML document in blob form
        /// </summary>
        public static XDocument AddXElementToXDocument(BlobClient xDocuBlobClient, XElement newElement)
        {
            //get person list from storage
            var personListXml = BlobClientToXml(xDocuBlobClient);

            //add new person to list
            personListXml.Root.Add(newElement);

            return personListXml;
        }

        /// <summary>
        /// Extracts data coming in from API caller
        /// Note : Data is assumed to be XML in string form
        /// </summary>
        public static XElement ExtractDataFromRequest(HttpRequestMessage request)
        {
            //get xml string from caller
            var xmlString = RequestToXmlString(request);

            //parse xml string
            var xml = XElement.Parse(xmlString);

            return xml;
        }

        /// <summary>
        /// Converts a blob client of a file to an XML document
        /// </summary>
        public static XDocument BlobClientToXml(BlobClient blobClient)
        {
            //parse string as xml doc
            var document = XDocument.Load(blobClient.Download().Value.Content);

            return document;
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// When receiving
        /// Extracts the raw data coming from the server, and extract what is needed
        ///  here original data is overwritten
        /// </summary>
        public static string RequestToXmlString(HttpRequestMessage rawData)
        {
            //get request body
            return rawData.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Extracts names from the query URL
        /// </summary>
        public static async Task<object> ExtractNames(HttpRequest request)
        {
            string male = request.Query["male"];
            string female = request.Query["female"];

            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            male = male ?? data?.male;
            female = female ?? data?.female;

            return new { Male = male, Female = female };

        }

        public static CompatibilityReport GetCompatibilityReport(string maleName, string femaleName, Data personList)
        {
            //get all the people
            var peopleList = DatabaseManager.GetPersonList(personList);

            //filter out the male and female ones we want
            var male = peopleList.Find(person => person.GetName() == maleName);
            var female = peopleList.Find(person => person.GetName() == femaleName);

            return MatchCalculator.GetCompatibilityReport(male, female);
        }

    }
}