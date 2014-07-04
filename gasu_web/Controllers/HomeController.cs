using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;

namespace gasu_web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpGet]
        public string SendDataToGASU()
        {
            if (Request.IsAjaxRequest())
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
                xmlDoc.Load(@"E:\Work\git\GASU_SMEV\template_web.xml");

                var elem = xmlDoc.GetElementsByTagName("s:Body");

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

                return System.IO.File.ReadAllText(@"E:\Work\git\GASU_SMEV\template_tmp.xml");
            }
            return null;
        }

        public ActionResult SendDataToGASU(string data1)
        {
            if (Request.IsAjaxRequest())
            {

            }
            return null;
        }
    }
}
