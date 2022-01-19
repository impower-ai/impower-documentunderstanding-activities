using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Impower.DocumentUnderstanding.Validation
{
    public class InvalidVinException : Exception
    {
        public InvalidVinException(string message) : base(message)
        {
        }
    }

    internal static class ValidationExtensions
    {
        public static readonly string[] VinQueryFilter = {
                "Error Code",
                "Error Text",
                "Make",
                "Manufacturer Name",
                "Model",
                "Model Year",
                "Trim",
                "Vehicle Type",
                "Doors",
                "Body Class"
        };
        internal static JObject GetVinInfoFromNHTSA(string vin)
        {
            HttpWebResponse response = null;
            var url = "https://vpic.nhtsa.dot.gov/api/vehicles/decodevin/" + HttpUtility.UrlEncode(vin) + "?format=json";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Accept = "*/*";
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(
                retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromSeconds(3)
            );
            retryPolicy.Execute(() =>
            {
                Console.WriteLine($"Validating VIN ({vin}) against NHTSA database...");
                response = (HttpWebResponse)httpWebRequest.GetResponse();
            });
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                return JObject.Parse(streamReader.ReadToEnd());
            }
        }

        public static bool ValidateVinWithNHTSA(string vin, out Dictionary<string, string> properties)
        {
            if(vin.Length != 17) {
                properties = null;
                return false;
            }
            JObject response = GetVinInfoFromNHTSA(vin);
            JArray values = response["Results"] as JArray;
            properties = values.Select(
                jToken => jToken as JObject
            ).Where(
                jObject => VinQueryFilter.Contains(jObject["Variable"].ToString())
            ).ToDictionary(
                jObject => jObject["Variable"].ToString(),
                jObject => jObject["Value"].ToString()
            );
            return properties["Error Code"].ToString() == "0";
        }
    }

    [DisplayName("Validate And Clean VIN")]
    public class ValidateAndCleanVIN : CodeActivity
    {
        private readonly Regex vinRegex = new Regex(@"[^A-HJ-NPR-Za-hj-npr-z\d]");

        [Category("Input")]
        [DisplayName("VIN Number")]
        [RequiredArgument]
        public InOutArgument<string> VIN { get; set; }

        [Category("Input")]
        [DisplayName("Attempt To Fix OCR?")]
        [Description("Replace I's, O's, and Q's to attempt to fix incorrect OCR.")]
        [DefaultValue(true)]
        public InArgument<bool> FixOCR { get; set; }

        [Category("Output")]
        [DisplayName("VIN Properties")]
        [Description("List of properties associated with VIN, per NHTSA")]
        public OutArgument<Dictionary<string,string>> Properties { get; set; }

        [Category("Output")]
        [DisplayName("Valid VIN?")]
        [Description("Indicates if the VIN was validated.")]
        public OutArgument<bool> Valid { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            Dictionary<string, string> properties;
            string sanitizedVin = VIN.Get(context).ToUpper();
            Console.WriteLine($"Sanitized VIN: '{sanitizedVin}'");
            if (FixOCR.Get(context))
            {
                sanitizedVin = sanitizedVin.Replace("I", "1").Replace("O", "0").Replace("Q", "9");
            }
            sanitizedVin = vinRegex.Replace(sanitizedVin, String.Empty);
            bool valid = ValidationExtensions.ValidateVinWithNHTSA(sanitizedVin, out properties);
            VIN.Set(context, sanitizedVin);
            Properties.Set(context, properties);
            Valid.Set(context, valid);
        }
    }
}