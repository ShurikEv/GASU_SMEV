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

namespace Samples.Xml.cs
{
    class SignSmevRequest
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Разбираем аргументы
            if (args.Length < 2)
            {
                Console.WriteLine("<doc_to_sign> <signed_doc>");
                return;
            }

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
            SignXmlFile(args[0], args[1], certs[0]);

            // Проверяем подпись
            VerifyXmlFile(args[1]);
        }

        // Подписывает файл XML с помощью заданного сертификата и сохраняет подписанный документ
        // в новый файл.
        static void SignXmlFile(string FileName, string SignedFileName, X509Certificate2 Certificate)
        {
            // Создаем новый документ XML.
            XmlDocument doc = new XmlDocument();

            // Читаем документ из файла.
            doc.Load(new XmlTextReader(FileName));

            // Создаём объект SmevSignedXml - наследник класса SignedXml с перегруженным GetIdElement
            // для корректной обработки атрибута wsu:Id. 
            SmevSignedXml signedXml = new SmevSignedXml(doc);

            // Задаём ключ подписи для документа SmevSignedXml.
            signedXml.SigningKey = Certificate.PrivateKey;

            // Создаем ссылку на подписываемый узел XML. В данном примере и в методических
            // рекомендациях СМЭВ подписываемый узел soapenv:Body помечен идентификатором "body".
            Reference reference = new Reference();
            reference.Uri = "#body";

            // Задаём алгоритм хэширования подписываемого узла - ГОСТ Р 34.11-94. Необходимо
            // использовать устаревший идентификатор данного алгоритма, т.к. именно такой
            // идентификатор используется в СМЭВ.
#pragma warning disable 612
            //warning CS0612: 'CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3411UrlObsolete' is obsolete
            reference.DigestMethod = CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3411UrlObsolete;
#pragma warning restore 612

            // Добавляем преобразование для приведения подписываемого узла к каноническому виду
            // по алгоритму http://www.w3.org/2001/10/xml-exc-c14n# в соответствии с методическими
            // рекомендациями СМЭВ.
            XmlDsigExcC14NTransform c14 = new XmlDsigExcC14NTransform();
            reference.AddTransform(c14);

            // Добавляем ссылку на подписываемый узел.
            signedXml.AddReference(reference);

            // Задаём преобразование для приведения узла ds:SignedInfo к каноническому виду
            // по алгоритму http://www.w3.org/2001/10/xml-exc-c14n# в соответствии с методическими
            // рекомендациями СМЭВ.
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            // Задаём алгоритм подписи - ГОСТ Р 34.10-2001. Необходимо использовать устаревший
            // идентификатор данного алгоритма, т.к. именно такой идентификатор используется в
            // СМЭВ.
#pragma warning disable 612
            //warning CS0612: 'CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3411UrlObsolete' is obsolete
            signedXml.SignedInfo.SignatureMethod = CryptoPro.Sharpei.Xml.CPSignedXml.XmlDsigGost3410UrlObsolete;
#pragma warning restore 612

            // Вычисляем подпись.
            signedXml.ComputeSignature();

            // Получаем представление подписи в виде XML.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Добавляем необходимые узлы подписи в исходный документ в заготовленное место.
            doc.GetElementsByTagName("ds:Signature")[0].PrependChild(
                doc.ImportNode(xmlDigitalSignature.GetElementsByTagName("SignatureValue")[0], true));
            doc.GetElementsByTagName("ds:Signature")[0].PrependChild(
                doc.ImportNode(xmlDigitalSignature.GetElementsByTagName("SignedInfo")[0], true));

            // Добавляем сертификат в исходный документ в заготовленный узел
            // wsse:BinarySecurityToken.
            doc.GetElementsByTagName("wsse:BinarySecurityToken")[0].InnerText =
                Convert.ToBase64String(Certificate.RawData);

            // Сохраняем подписанный документ в файл.
            using (XmlTextWriter xmltw = new XmlTextWriter(SignedFileName,
                new UTF8Encoding(false)))
            {
                doc.WriteTo(xmltw);
            }
        }

        // Проверяет подписи в файле XML.
        static void VerifyXmlFile(string SignedFileName)
        {
            // Создаем новый документ XML.
            XmlDocument xmlDocument = new XmlDocument();

            // Форматируем документ с сохранением всех пробельных символов, т.к. они
            // важны при проверке подписи.
            xmlDocument.PreserveWhitespace = true;

            // Загружаем подписанный документ XML из файла.
            xmlDocument.Load(SignedFileName);

            // Ищем все узлы ds:Signature и сохраняем их в объекте XmlNodeList
            XmlNodeList nodeList = xmlDocument.GetElementsByTagName(
                "Signature", SignedXml.XmlDsigNamespaceUrl);

            Console.WriteLine("Найдено подписей: {0}.", nodeList.Count);

            // Проверяем все подписи.
            for (int curSignature = 0; curSignature < nodeList.Count; curSignature++)
            {
                // Создаём объект SmevSignedXml - наследник класса SignedXml с перегруженным
                // GetIdElement для корректной обработки атрибута wsu:Id. 
                SmevSignedXml signedXml = new SmevSignedXml(xmlDocument);

                // Загружаем узел с подписью.
                signedXml.LoadXml((XmlElement)nodeList[curSignature]);

                // Получаем идентификатор ссылки на узел wsse:BinarySecurityToken,
                // содержащий сертификат подписи.
                XmlNodeList referenceList = signedXml.KeyInfo.GetXml().GetElementsByTagName(
                    "Reference", WSSecurityWSSENamespaceUrl);
                if (referenceList.Count == 0)
                {
                    throw new XmlException("Не удалось найти ссылку на сертификат");
                }

                // Ищем среди аттрибутов ссылку на сертификат.
                string binaryTokenReference = ((XmlElement)referenceList[0]).GetAttribute("URI");

                // Ссылка должна быть на узел внутри данного документа XML, т.е. она имеет вид
                // #ID, где ID - идентификатор целевого узла
                if (string.IsNullOrEmpty(binaryTokenReference) || binaryTokenReference[0] != '#')
                {
                    throw new XmlException("Не удалось найти ссылку на сертификат");
                }

                // Получаем узел BinarySecurityToken с закодированным в base64 сертификатом
                XmlElement binaryTokenElement = signedXml.GetIdElement(
                    xmlDocument, binaryTokenReference.Substring(1));
                if (binaryTokenElement == null)
                {
                    throw new XmlException("Не удалось найти сертификат");
                }

                // Создаём объект X509Certificate2
                X509Certificate2 cert =
                    new X509Certificate2(Convert.FromBase64String(binaryTokenElement.InnerText));

                // Проверяем подпись.
                // ВНИМАНИЕ! Проверка сертификата в данном примере не осуществляется. Её необходимо
                // реализовать самостоятельно в соответствии с требованиями к подписи проверяемого
                // типа сообщения СМЭВ.
                bool result = signedXml.CheckSignature(cert.PublicKey.Key);

                // Выводим результат проверки подписи в консоль
                if (result)
                {
                    Console.WriteLine("Подпись №{0} верна.", curSignature + 1);
                }
                else
                {
                    Console.WriteLine("Подпись №{0} не верна.", curSignature + 1);
                }
            }
        }

        // Класс SmevSignedXml - наследник класса SignedXml с перегруженным
        // GetIdElement для корректной обработки атрибута wsu:Id. 
        class SmevSignedXml : SignedXml
        {
            public SmevSignedXml(XmlDocument document)
                : base(document)
            {
            }

            public override XmlElement GetIdElement(XmlDocument document, string idValue)
            {
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
                nsmgr.AddNamespace("wsu", WSSecurityWSUNamespaceUrl);
                return document.SelectSingleNode("//*[@wsu:Id='" + idValue + "']", nsmgr) as XmlElement;
            }
        }

        public const string WSSecurityWSSENamespaceUrl = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        public const string WSSecurityWSUNamespaceUrl = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    }
}
