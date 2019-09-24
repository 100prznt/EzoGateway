using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace EzoGateway.Server
{
    public class HttpResource
    {
        #region Members
        string m_LocalPath;

        #endregion Members

        #region Properties
        public InternetMediaType Mime { get; private set; }


        public Dictionary<string, string> HeaderFields { get; set; }

        public string StatusCode { get; set; }

        public string BodyTextContent { get; set; }

        public StorageFile File { get; set; }

        /// <summary>
        /// Bodycontent is binary (no text)
        /// </summary>
        public bool IsBinary { get => Mime.IsBinary(); }

        public string LocalPath
        {
            get => m_LocalPath;
            set
            {
                m_LocalPath = value;
                ApplyContentType(value.Substring(value.LastIndexOf('.')));
            }
        }

        #region Http errors
        /// <summary>
        /// Bad Request
        /// </summary>
        /// <returns></returns>
        public static HttpResource Error400 =>
            new HttpResource()
            {
                StatusCode = "400 Bad Request",
                Mime = InternetMediaType.TextHtml,
                BodyTextContent = $"<!DOCTYPE html>\r\n<html>\r\n<h1>400 Bad Request</h1>\r\n</html>"
            };

        /// <summary>
        /// Not Found
        /// </summary>
        /// <returns></returns>
        public static HttpResource Error404 =>
            new HttpResource()
            {
                StatusCode = "404 Not Found",
                Mime = InternetMediaType.TextHtml,
                BodyTextContent = $"<!DOCTYPE html>\r\n<html>\r\n<h1>404 Not Found</h1>\r\n</html>"
            };

        /// <summary>
        /// Not Found
        /// </summary>
        /// <returns></returns>
        public static HttpResource Error405 =>
            new HttpResource()
            {
                StatusCode = "405 Method Not Allowed",
                Mime = InternetMediaType.TextHtml,
                BodyTextContent = $"<!DOCTYPE html>\r\n<html>\r\n<h1>405 Method Not Allowed</h1>\r\n</html>"
            };
        #endregion Http errors

        #endregion Properties

        #region Constructor
        /// <summary>
        /// Empty constructor
        /// </summary>
        public HttpResource()
        {
            HeaderFields = new Dictionary<string, string>();
        }

        public HttpResource(string fileName) : this()
        {
            LocalPath = fileName;
        }

        public HttpResource(StorageFile file) : this()
        {
            if (file != null)
            {
                StatusCode = "200 OK";
                LocalPath = file.Path;
                File = file;
            }
        }

        #endregion Constructor

        #region Services

        public string GetHeader(int contentLength = 0)
        {
            if (contentLength == 0)
                contentLength = BodyTextContent == null ? 0 : BodyTextContent.Length;


            var header = new StringBuilder();
            header.AppendLine("HTTP/1.1 " + StatusCode);
            header.AppendLine("Content-Length: " + contentLength);
            if (Mime != InternetMediaType.NotSpecified)
                header.AppendLine("Content-Type: " + Mime.GetContentType());
            foreach (var field in HeaderFields)
                header.AppendLine($"{field.Key}: {field.Value}");
            header.AppendLine("Connection: close");
            header.AppendLine();

            return header.ToString();
        }

        public static HttpResource CreateHtmlResource(string html) =>
            new HttpResource()
            {
                StatusCode = "200 OK",
                Mime = InternetMediaType.TextHtml,
                BodyTextContent = $"<!DOCTYPE html>\r\n<html>\r\n{html}\r\n</html>"
            };

        public static HttpResource CreateHtmlResource(string title, string body, string style = "") =>
            new HttpResource()
            {
                StatusCode = "200 OK",
                Mime = InternetMediaType.TextHtml,
                BodyTextContent = $"<!DOCTYPE html>\r\n<html>\r\n<head>\r\n<meta charset=\"utf - 8\">\r\n<title>{title}</title>\r\n<style>{style}</style>\r\n</head>\r\n<body>\r\n{body}\r\n</body>\r\n</html>"
            };

        public static HttpResource CreateHtmlResource(string title, Dictionary<string, string> data)
        {
            var style = new StringBuilder();
            style.AppendLine("table { font - family: arial, sans-serif; border-collapse: collapse; width: 100 %; }");
            style.AppendLine("td, th { border: 1px solid #dddddd; text-align: left; padding: 8px; }");
            style.AppendLine("tr:nth-child(even) { background-color: #dddddd; }");

            var body = new StringBuilder();
            body.AppendLine($"<h1>{title}</h1>");
            body.AppendLine("<table style=\"width: 100 % \">");
            body.AppendLine("<tr>");
            body.AppendLine("<th>#</th>");
            body.AppendLine("<th>Key</th>");
            body.AppendLine("<th>Value</th>");
            body.AppendLine("</tr>");
            int i = 1;
            foreach (var kvp in data)
            {
                body.AppendLine("<tr>");
                body.AppendLine($"<td>{i}</td>");
                body.AppendLine($"<td>{kvp.Key}</td>");
                body.AppendLine($"<td>{kvp.Value}</td>");
                body.AppendLine("</tr>");
                i++;
            }
            body.AppendLine("</table>");

            return CreateHtmlResource(title, body.ToString(), style.ToString());
        }

        public static HttpResource CreateJsonResource(object data) =>
            new HttpResource()
            {
                StatusCode = "200 OK",
                Mime = InternetMediaType.ApplicationJson,
                BodyTextContent = JsonConvert.SerializeObject(data ,Formatting.Indented, new JsonSerializerSettings() { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii })
            };

        public static HttpResource JsonAccepted202(string jobName) =>
            new HttpResource()
            {
                StatusCode = "202 Accepted",
                Mime = InternetMediaType.ApplicationJson,
                BodyTextContent = JsonConvert.SerializeObject(new { task = new RestTask(1, jobName) }, Formatting.Indented)
            };

        /// <summary>
        /// HTTP 423 - Locked
        /// </summary>
        /// <param name="error">Error description</param>
        /// <returns></returns>
        public static HttpResource JsonAccepted423(string error) =>
            new HttpResource()
            {
                StatusCode = "423 Locked",
                Mime = InternetMediaType.ApplicationJson,
                BodyTextContent = JsonConvert.SerializeObject(new { reason = error }, Formatting.Indented)
            };

        #endregion Services

        #region Internal services
        private void ApplyContentType(string fileExtension)
        {
            foreach (InternetMediaType mime in Enum.GetValues(typeof(InternetMediaType)))
            {
                if (mime.GetFileExtensions().Any(x => x.Equals(fileExtension.Trim('.'), StringComparison.OrdinalIgnoreCase)))
                {
                    Mime = mime;
                    return;
                }
            }
        }

        #endregion Internal services
    }
}
