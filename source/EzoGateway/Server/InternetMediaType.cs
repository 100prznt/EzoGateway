using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Server
{
    /// <summary>
    /// The Internet Media Type or MIME-Type (Multipurpose Internet Mail Extension) specifies what type of data is sent. 
    /// </summary>
    public enum InternetMediaType
    {
        NotSpecified = 0,
        /// <summary>
        /// Contains a string in JavaScript object notation.
        /// </summary>
        [FileExtension("json")]
        ApplicationJson,
        /// <summary>
        /// XML-Files 
        /// </summary>
        [FileExtension("xml")]
        ApplicationXml,
        /// <summary>
        /// PNG-Files 
        /// </summary>
        [IsBinary]
        [FileExtension("png")]
        ImagePng,
        /// <summary>
        /// JPEG-Files 
        /// </summary>
        [FileExtension("jpeg", "jpg", "jpe")]
        ImageJpeg,
        /// <summary>
        /// SVG-Files 
        /// </summary>
        [Mime("image/svg+xml")]
        [IsBinary]
        [FileExtension("svg")]
        ImageSvgXml,
        /// <summary>
        /// comma separated data files 
        /// </summary>
        [Mime("text/comma-separated-values")]
        [FileExtension("csv")]
        TextCommaSeparatedValues,
        /// <summary>
        /// CSS Stylesheet-Files  
        /// </summary>
        [FileExtension("css")]
        TextCss,
        /// <summary>
        /// HTML-Files 
        /// </summary>
        [FileExtension("htm", "html", "shtml")]
        TextHtml,
        /// <summary>
        /// JavaScript-Files 
        /// </summary>
        [FileExtension("js")]
        TextJavascript,
        /// <summary>
        /// plain text
        /// </summary>
        [FileExtension("txt")]
        TextPlain,


    }
}
