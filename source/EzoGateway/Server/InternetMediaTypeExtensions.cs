using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Server
{
    public static class InternetMediaTypeExtensions
    {
        public static string GetContentType(this InternetMediaType mime)
        {
            Attribute[] attributes = mime.GetAttributes();

            MimeAttribute attr = null;

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType() == typeof(MimeAttribute))
                {
                    attr = (MimeAttribute)attributes[i];
                    break;
                }
            }

            if (attr == null)
            {
                string contentType = "";
                foreach (char c in mime.ToString())
                {
                    if (Char.IsUpper(c) && contentType.Length > 0)
                        contentType += "/";

                    contentType += c;
                }
                return contentType.ToLower();
            }
            else
                return attr.ContentType;
        }

        public static string[] GetFileExtensions(this InternetMediaType mime)
        {
            Attribute[] attributes = mime.GetAttributes();

            FileExtensionAttribute attr = null;

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType() == typeof(FileExtensionAttribute))
                {
                    attr = (FileExtensionAttribute)attributes[i];
                    break;
                }
            }

            if (attr == null)
            {
                return new string[0];
            }
            else
                return attr.FileExtensions;
        }

        public static bool IsBinary(this InternetMediaType mime)
        {
            Attribute[] attributes = mime.GetAttributes();

            IsBinaryAttribute attr = null;

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType() == typeof(IsBinaryAttribute))
                {
                    attr = (IsBinaryAttribute)attributes[i];
                    break;
                }
            }

            if (attr == null)
            {
                return false;
            }
            else
                return attr.IsBinary;
        }

        private static Attribute[] GetAttributes(this InternetMediaType mime)
        {
            var fi = mime.GetType().GetField(mime.ToString());
            Attribute[] attributes = (Attribute[])fi.GetCustomAttributes(typeof(Attribute), false);

            return attributes;
        }
    }
}
