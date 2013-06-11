using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Iroha.WebPages.Models
{
    public class ContentPageMetadata
    {
        public String Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public String CreatedBy { get; set; }
        public String ModifiedBy { get; set; }

        private static XmlSerializer _serializer;
        static ContentPageMetadata()
        {
            _serializer = new XmlSerializer(typeof(ContentPageMetadata));
        }

        public static ContentPageMetadata LoadFromFile(String path)
        {
            using (var reader = new XmlTextReader(path))
            {
                return _serializer.Deserialize(reader) as ContentPageMetadata;
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