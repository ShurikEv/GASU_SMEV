using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace sign_xml
{
    class Program
    {
        static void Main(string[] args)
        {
            Generate();
            Prepare();
            Send();

            File.Delete(@"E:\Work\git\GASU_SMEV\template_tmp.xml");
            File.Delete(@"E:\Work\git\GASU_SMEV\template_tmp1.xml");
        }

        private static void Generate()
        {
            #region msg
            var msg = new GASU.GasuMessage
            {
                Message = new GASU.MessageType
                {
                    Sender = new GASU.orgExternalType
                    {
                        Code = "XXXX11111",
                        Name = "Текстовое наименование участника межведомственного обмена, являющегося владельцем информационной системы."
                    },
                    Recipient = new GASU.orgExternalType
                    {
                        Code = "XXXX11111",
                        Name = "Текстовое наименование участника межведомственного обмена, являющегося владельцем информационной системы."
                    },
                    Originator = new GASU.orgExternalType
                    {
                        Code = "XXXX11111",
                        Name = "Текстовое наименование участника межведомственного обмена, являющегося владельцем информационной системы."
                    },
                    ServiceName = "",
                    TypeCode = GASU.TypeCodeType.GFNC,
                    Status = GASU.StatusType.REQUEST,
                    Date = DateTime.Now,
                    ExchangeType = "XXXX11111",
                    RequestIdRef = "XXXX11111",
                    OriginRequestIdRef = "XXXX11111",
                    ServiceCode = "XXXX11111",
                    CaseNumber = "XXXX11111",
                    TestMsg = "",
                },
                MessageData = new GASU.GasuMessageMessageData
                {
                    AppData = new GASU.GasuMessageMessageDataAppData
                    {
                        AppMessage = new GASU.AppMessageType
                        {
                            AppHeader = new GASU.AppHeaderType
                            {
                                ID = "12345",
                                DataSourceRef = "8765",
                                HeaderInfo = new GASU.AppDataType()
                            },
                            MessageType = GASU.MessageTypeType.ImportFull,
                            Body = new GASU.AppMessageTypeBody
                            {
                                Items = new object[2]
                            }
                        }
                    }
                }
            };
            #endregion



            using (var stream = new StreamWriter(@"E:\Work\git\GASU_SMEV\template_tmp.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GASU.GasuMessage));
                serializer.Serialize(stream, msg);
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@"E:\Work\git\GASU_SMEV\template.xml");

            var elem = xmlDoc.GetElementsByTagName("SOAP-ENV:Body");

            var xmlDoc2 = new XmlDocument();
            xmlDoc2.Load(@"E:\Work\git\GASU_SMEV\template_tmp.xml");

            var subElem = xmlDoc2.LastChild;


            var node = subElem.OuterXml;

            // стандартный десериализатор добавляет какую-то левую фигню
            node = node.Replace(@" xmlns=" + "\"" + "http://smev.gosuslugi.ru/rev120315" + "\"", "");

            node = node.Replace("xmlns:xsi=" + "\"" + "http://www.w3.org/2001/XMLSchema-instance" + "\" " + "xmlns:xsd=" + "\"" + "http://www.w3.org/2001/XMLSchema" + "\"",
                @" xmlns=" + "\"" + "http://smev.gosuslugi.ru/rev120315" + "\"");

            elem[0].InnerXml = node;

            xmlDoc2.PreserveWhitespace = false;

            xmlDoc.Save(@"E:\Work\git\GASU_SMEV\template_tmp.xml");
        }

        static void Send()
        {
            WebRequest req = null;
            WebResponse rsp = null;
            try
            {
                string fileName = @"E:\Work\git\GASU_SMEV\template_tmp1.xml";
                string uri = @"http://188.254.16.92:7777/gateway/services/SID0003565?wsdl";
                req = WebRequest.Create(uri);
                //req.Proxy = WebProxy.GetDefaultProxy(); // Enable if using proxy
                req.Method = "POST";        // Post method
                req.ContentType = "application/soap+xml";     // content type

                req.Headers.Add("Accept-Encoding: gzip, deflate");
                req.Headers.Add("SOAPAction: " + "\"" + "http://www.government.ru/gasu2/publish" + "\"");

                // Wrap the request stream with a text-based writer
                StreamWriter writer = new StreamWriter(req.GetRequestStream());
                // Write the XML text into the stream
                writer.WriteLine(GetTextFromXMLFile(fileName));
                writer.Close();
                // Send the data to the webserver
                rsp = req.GetResponse(); //I am getting error over here
                StreamReader sr = new StreamReader(rsp.GetResponseStream());
                string result = sr.ReadToEnd();
                sr.Close();

            }
            catch (WebException webEx)
            {

            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (req != null) req.GetRequestStream().Close();
                if (rsp != null) rsp.GetResponseStream().Close();
            }
        }

        private static string GetTextFromXMLFile(string file)
        {
            StreamReader reader = new StreamReader(file);
            string ret = reader.ReadToEnd();
            reader.Close();
            return ret;
        }

        static void Prepare()
        {
            X509Store certStore = new X509Store(StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certs = X509Certificate2UI.SelectFromCollection(
                certStore.Certificates,
                "Выберите сертификат",
                "Пожалуйста, выберите сертификат электронной подписи",
                X509SelectionFlag.SingleSelection);

            if (certs.Count == 0)
            {
                Console.WriteLine("Сертификат не выбран.");
                return;
            }

            // Подписываем запрос
            SignXmlFile(@"E:\Work\git\GASU_SMEV\template_tmp.xml", @"E:\Work\git\GASU_SMEV\template_tmp1.xml", certs[0]);
        }

        static void SignXmlFile(string FileName, string SignedFileName, X509Certificate2 Certificate)
        {
            System.Xml.XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;

            using (var txtReader = new XmlTextReader(FileName))
            {
                doc.Load(txtReader);
            }

            // Класс с перегруженным GetIdElement для корректной обработки wsu:Id
            MySignedXml signedXml = new MySignedXml(doc);
            signedXml.SigningKey = Certificate.PrivateKey;
            Reference reference = new Reference();
            reference.Uri = "#body";
#pragma warning disable 612
            //warning CS0612: 'CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3411UrlObsolete' is obsolete

            reference.DigestMethod = CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3411UrlObsolete;

#pragma warning restore 612

            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            XmlDsigExcC14NTransform c14 = new XmlDsigExcC14NTransform();
            reference.AddTransform(c14);
            signedXml.AddReference(reference);
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(Certificate));
            signedXml.KeyInfo = keyInfo;
            signedXml.SignedInfo.CanonicalizationMethod = c14.Algorithm;

#pragma warning disable 612
            //warning CS0612: 'CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3411UrlObsolete' is obsolete

            signedXml.SignedInfo.SignatureMethod = CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3410UrlObsolete;

#pragma warning restore 612

            signedXml.ComputeSignature();
            XmlElement xmlDigitalSignature = signedXml.GetXml();
            doc.GetElementsByTagName("ds:Signature")[0].PrependChild(doc.ImportNode(xmlDigitalSignature.GetElementsByTagName("SignatureValue")[0], true));
            doc.GetElementsByTagName("ds:Signature")[0].PrependChild(doc.ImportNode(xmlDigitalSignature.GetElementsByTagName("SignedInfo")[0], true));
            doc.GetElementsByTagName("wsse:BinarySecurityToken")[0].InnerText = xmlDigitalSignature.GetElementsByTagName("X509Certificate")[0].InnerText;

            using (XmlTextWriter xmltw = new XmlTextWriter(SignedFileName,
                new UTF8Encoding(false)))
            {
                doc.WriteTo(xmltw);
            }

            
        }

        static void VerifyXmlFile(string SignedFileName)
        {
            // Создаем новый XML документ в памяти.
            XmlDocument xmlDocument = new XmlDocument();

            // Сохраняем все пробельные символы, они важны при проверке
            // подписи.
            xmlDocument.PreserveWhitespace = true;

            // Загружаем подписанный документ из файла.
            xmlDocument.Load(SignedFileName);

            // Ищем все node "Signature" и сохраняем их в объекте XmlNodeList
            XmlNodeList nodeList = xmlDocument.GetElementsByTagName(
                "Signature", SignedXml.XmlDsigNamespaceUrl);

            Console.WriteLine("Найдено:{0} подпис(ей).", nodeList.Count);

            // Проверяем все подписи.
            for (int curSignature = 0; curSignature < nodeList.Count; curSignature++)
            {
                // Создаем объект SignedXml для проверки подписи документа.
                MySignedXml signedXml = new MySignedXml(xmlDocument);

                // Загружаем узел с подписью.
                signedXml.LoadXml((XmlElement)nodeList[curSignature]);

                // SignXml самостоятельно не найдет сертификат подписи
                // т.к. он лежит вне узла Signature.
                // Поэтому самостоятельно извлечем сертификат по ссылке из KeyInfo
                // и явно зададим открытый ключ для проверки подписи.
                XmlNodeList referenceList = signedXml.KeyInfo.GetXml().GetElementsByTagName(
                                                                                    "Reference",
                                                                                    "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");

                if (referenceList.Count == 0)
                {
                    throw new XmlException("Не удалось найти ссылку на сертификат");
                }

                // Ищем среди аттрибутов ссылку на сертификат.
                string binaryTokenReference = null;
                foreach (XmlAttribute attribute in referenceList[0].Attributes)
                {
                    if (attribute.Name.ToUpper().Equals("URI"))
                    {
                        // Получаем ссылку на сертификат.
                        // формат #Value. '#' - нужно выбросить.
                        binaryTokenReference = attribute.Value.Substring(1);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(binaryTokenReference))
                {
                    throw new XmlException("Не удалось найти ссылку на сертификат");
                }

                // Получаем узел BinarySecurityToken с закодированным в base64 сертификатом
                XmlElement binaryTokenElement = signedXml.GetIdElement(xmlDocument, binaryTokenReference);

                if (binaryTokenElement == null)
                {
                    throw new XmlException("Не удалось найти сертификат");
                }

                // Декодируем сертификат
                byte[] certBytes = Convert.FromBase64String(binaryTokenElement.InnerText);
                X509Certificate2 cert = new X509Certificate2();
                cert.Import(certBytes);

                // Проверяем подпись и выводим результат.
                bool result = signedXml.CheckSignature(cert.PublicKey.Key);

                // Выводим результат проверки подписи в консоль.
                if (result)
                    Console.WriteLine("XML подпись[{0}] верна.", curSignature + 1);
                else
                    Console.WriteLine("XML подпись[{0}] не верна.", curSignature + 1);
            }
        }

        //Класс MySignedXml
        class MySignedXml : SignedXml
        {
            public MySignedXml(XmlDocument document)
                : base(document)
            {
            }
            public override XmlElement GetIdElement(XmlDocument document, string idValue)
            {
                XmlNameTable myXmlNameTable = new NameTable();
                XmlNamespaceManager myNamespacemanager = new XmlNamespaceManager(myXmlNameTable);
                myNamespacemanager.AddNamespace("wsu",
                        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
                XmlNodeList lst = document.SelectNodes("//*[@wsu:Id='" + idValue + "' or @wsu:ID='" + idValue +
                        "' or @wsu:ID='" + idValue + "']", myNamespacemanager);
                if (lst.Count != 1)
                    return null;
                return (XmlElement)lst.Item(0);
            }
        }
    }
}
