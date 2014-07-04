using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace gasu_web
{
    public class XmlValueProviderFactory : ValueProviderFactory
    {

        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            var deserializedXml = GetDeserializedXml(controllerContext);

            if (deserializedXml == null) return null;

            var backingStore = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            AddToBackingStore(backingStore, string.Empty, deserializedXml.Root);

            return new DictionaryValueProvider<object>(backingStore, CultureInfo.CurrentCulture);

        }

        private static void AddToBackingStore(Dictionary<string, object> backingStore, string prefix, XElement xmlDoc)
        {
            // Check the keys to see if this is an array or an object
            var uniqueElements = new List<String>();
            var totalElments = 0;
            foreach (XElement element in xmlDoc.Elements())
            {
                if (!uniqueElements.Contains(element.Name.LocalName))
                    uniqueElements.Add(element.Name.LocalName);
                totalElments++;
            }

            var isArray = (uniqueElements.Count == 1 && totalElments > 1);


            // Add the elements to the backing store
            var elementCount = 0;
            foreach (XElement element in xmlDoc.Elements())
            {
                if (element.HasElements)
                {
                    if (isArray)
                        AddToBackingStore(backingStore, MakeArrayKey(prefix, elementCount), element);
                    else
                        AddToBackingStore(backingStore, MakePropertyKey(prefix, element.Name.LocalName), element);
                }
                else
                {
                    backingStore.Add(MakePropertyKey(prefix, element.Name.LocalName), element.Value);
                }
                elementCount++;
            }
        }


        private static string MakeArrayKey(string prefix, int index)
        {
            return prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        }

        private static string MakePropertyKey(string prefix, string propertyName)
        {
            if (!string.IsNullOrEmpty(prefix))
                return prefix + "." + propertyName;
            return propertyName;
        }

        private XDocument GetDeserializedXml(ControllerContext controllerContext)
        {
            var contentType = controllerContext.HttpContext.Request.ContentType;
            if (!contentType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase) &&
                !contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase))
                return null;

            XDocument xml;
            try
            {
                var xmlReader = new XmlTextReader(controllerContext.HttpContext.Request.InputStream);
                xml = XDocument.Load(xmlReader);
            }
            catch (Exception)
            {
                return null;
            }

            if (xml.FirstNode == null)//no xml.
                return null;

            return xml;
        }
    }
}
