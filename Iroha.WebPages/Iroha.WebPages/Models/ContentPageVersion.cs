using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Iroha.WebPages.Models
{
    public class ContentPageVersion
    {
        public ContentPageMetadata Metadata { get; set; }
        public String Content { get; set; }

        private static XmlSerializer _serializer;
        static ContentPageVersion()
        {
            _serializer = new XmlSerializer(typeof(ContentPageVersion));
        }

        public static ContentPageVersion LoadFromFile(String path)
        {
            using (var reader = new XmlTextReader(path))
            {
                return _serializer.Deserialize(reader) as ContentPageVersion;
            }
        }
        public void Save(String path)
        {
            using (var stream = File.Create(path))
            {
                _serializer.Serialize(stream, this);
            }
        }
    }
}