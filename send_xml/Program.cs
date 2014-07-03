// Copyright (C) 2006-2012 Крипто-Про. Все права защищены.
//
// Этот файл содержит информацию, являющуюся
// собственностью компании Крипто-Про.
// 
// Любая часть этого файла не может быть скопирована,
// исправлена, переведена на другие языки,
// локализована или модифицирована любым способом,
// откомпилирована, передана по сети с или на
// любую компьютерную систему без предварительного
// заключения соглашения с компанией Крипто-Про.
// 
// Программный код, содержащийся в этом файле, предназначен
// исключительно для целей обучения и не может быть использован
// для защиты информации.
// 
// Компания Крипто-Про не несет никакой
// ответственности за функционирование этого кода.

// Пример демонстрирует:
//  1) создания подписанного запроса к сервису СМЭВ.
//  2) проверку подписи под ответом сервиса СМЭВ.

using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Reflection;

namespace Samples.Xml.cs
{
    class SignSmevRequest
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Разбираем аргументы

            

            // Проверяем подпись
            //VerifyXmlFile(args[1]);
        }

        static void SignXmlFile(string FileName, string SignedFileName, X509Certificate2 Certificate)
        {
            System.Xml.XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;
            doc.Load(new XmlTextReader(FileName));

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
            SignXmlFile(@"E:\Work\git\GASU_SMEV\template.xml", @"E:\Work\git\GASU_SMEV\template1.xml", certs[0]);
        }

        static void Send()
        {
            WebRequest req = null;
            WebResponse rsp = null;
            try
            {
                string fileName = @"E:\Work\git\GASU_SMEV\template1.xml";
                string uri = @"http://188.254.16.92:7777/gateway/services/SID0003565?wsdl";
                req = WebRequest.Create(uri);
                //req.Proxy = WebProxy.GetDefaultProxy(); // Enable if using proxy
                req.Method = "POST";        // Post method
                req.ContentType = "text/xml";     // content type

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
    }
}
