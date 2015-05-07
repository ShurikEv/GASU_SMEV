using CryptoPro.Sharpei.ServiceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

/*
 * Доработанный пример отправки данных через СМЭВ взятый из 
 * примеров CryptoPRO .NET SDK (WCF/SMEV)
 */

namespace gasu_smev
{
    class Program
    {
        // слепок серверного сертификата с именем gasu.office.roskazna. Данный сертификат вместе со всей 
        // цепочкой УЦ должен быть получен от ГАСУ и должен быть импортирован в личное хранилище. Сертификаты
        // из цепочки должны быть проиимпортиромпортированы в доверенные корневые центры сертификации.
        private static string serverCertThumbprint = @"‎‎‎‎‎6832f8c782d6356db6adf450998de50c8ec5227f"; 

        // сертификат подписи ЭС. Должен быть импортирован в личное хранилище а так же содержать всю
        // цепочку УЦ. Сертификаты УЦ так же должны быть проимпортированы.
        private static string clientCertThumbprint = @"e9a715b1b30dd93c707e4f6ecfd2549fd6d9d78c";

        // адресс тестового сервиса ГАСУ в интернет
        //private static string serviceUri = @"http://gasu-office.roskazna.ru:80/Gasu2WSTest/gasu2SOAP";

        // адресс промышленного сервиса в интернет
        private static string serviceUri = @"https://gasu-office.roskazna.ru/Gasu2WSp2p/gasu2SOAP?wsdl";

        static void Main(string[] args)
        {
            var client = new GASU.gasu2Client();

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var coll = store.Certificates.Cast<X509Certificate2>().ToArray();

            X509Certificate2 clientCert = coll.FirstOrDefault(x => string.Equals(x.Thumbprint, clientCertThumbprint, StringComparison.InvariantCultureIgnoreCase));
            X509Certificate2 serverCert = coll.FirstOrDefault(x => string.Equals(x.Thumbprint, serverCertThumbprint, StringComparison.InvariantCultureIgnoreCase));

            client.ClientCredentials.ClientCertificate.Certificate = clientCert;
            client.ClientCredentials.ServiceCertificate.DefaultCertificate = serverCert;

            client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
            client.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;


            string serverCommonName = serverCert.GetNameInfo(X509NameType.SimpleName, false);
            // DNS имя не совпадает с CommonName из сертификата сервера. Поэтому явно задаем доверие.
            EndpointAddress myEndpointAddr = new EndpointAddress(new Uri(serviceUri),
                                                                EndpointIdentity.CreateDnsIdentity(
                                                                serverCommonName));
            client.Endpoint.Address = myEndpointAddr;


            client.ChannelFactory.Endpoint.Contract.ProtectionLevel = ProtectionLevel.Sign;

            Publish(client, Guid.NewGuid().ToString());
            store.Close();
        }

        private static void Query(GASU.gasu2Client client, string msgId)
        {
            #region Query
            var msg = new GASU.GasuQueryMessage
            {
                Message = new GASU.MessageType
                {
                    Sender = new GASU.orgExternalType
                    {
                        Code = "<Код зарегистрированной системы-поставщика>",
                        Name = "<Наименование зарегистрированной системы-поставщика>"
                    },
                    Recipient = new GASU.orgExternalType
                    {
                        Code = "<Код зарегистрированной системы-поставщика>",
                        Name = "<Наименование зарегистрированной системы-поставщика>"
                    },
                    Originator = new GASU.orgExternalType
                    {
                        Code = "<Код зарегистрированной системы-поставщика>",
                        Name = "<Наименование зарегистрированной системы-поставщика>"
                    },
                    ServiceName = "",
                    TypeCode = GASU.TypeCodeType.GFNC,
                    Status = GASU.StatusType.REQUEST,
                    Date = DateTime.Now,
                    
                    RequestIdRef = msgId,
                    OriginRequestIdRef = msgId,
                    ServiceCode = "",
                    CaseNumber = "",
                    TestMsg = "",
                },
                MessageData = new GASU.MessageDataType
                {
                    AppData = new GASU.AppDataType
                    {
                        AppQueryMessage = new GASU.AppQueryMessageType
                        {
                            AppHeader = new GASU.AppHeaderType
                            {
                                DataSourceRef = "<Код зарегистрированной системы-поставщика>",
                                ID = "<Код зарегистрированной системы-поставщика>",
                                HeaderInfo = new GASU.AppDataType()
                            },
                            Query = new GASU.QueryType
                            {
                                returnType = GASU.ObjectTypeType.dataset,
                                pagingStartPage = 1,
                                pagingPageSize = 1000,
                                ItemElementName = GASU.ItemChoiceType.IndicatorQuery,
                                Item = new GASU.IndicatorQueryType
                                {
                                    providerRef = "GASU",
                                    Value = "ГАСУ/МУП/Р/1"
                                }
                            }
                        }
                    }
                }
            };

            client.query(msg);
            #endregion
        }

        private static void Publish(GASU.gasu2Client client, string msgId)
        {
            #region Publish
            var msg = new GASU.GasuMessage
            {
                Message = new GASU.MessageType
                {
                    Sender = new GASU.orgExternalType
                    {
                        Code = "<Код зарегистрированной системы-поставщика>",
                        Name = "<Наименование зарегистрированной системы-поставщика>"
                    },
                    Recipient = new GASU.orgExternalType
                    {
                        Code = "<Код зарегистрированной системы-поставщика>",
                        Name = "<Наименование зарегистрированной системы-поставщика>"
                    },
                    Originator = new GASU.orgExternalType
                    {
                        Code = "<Код зарегистрированной системы-поставщика>",
                        Name = "<Наименование зарегистрированной системы-поставщика>"
                    },
                    ServiceName = "",
                    TypeCode = GASU.TypeCodeType.GFNC,
                    Status = GASU.StatusType.REQUEST,
                    Date = DateTime.Now,
                    RequestIdRef = msgId,
                    OriginRequestIdRef = msgId,
                    ServiceCode = "",
                    CaseNumber = "",
                    TestMsg = "true"
                },
                MessageData = new GASU.GasuMessageMessageData
                {
                    AppData = new GASU.GasuMessageMessageDataAppData
                    {
                        AppMessage = new GASU.AppMessageType
                        {
                            AppHeader = new GASU.AppHeaderType
                            {
                                ID = "<Код зарегистрированной системы-поставщика>",
                                DataSourceRef = "<Код зарегистрированной системы-поставщика>",
                                HeaderInfo = new GASU.AppDataType()
                            },
                            MessageType = GASU.MessageTypeType.ImportFull,
                            Body = new GASU.AppMessageTypeBody
                            {
                                Items = new GASU.DataSetType[] 
                                {
                                    new GASU.DataSetType 
                                    {
                                        indicatorRef = "ГАСУ/МУП/Р/16",
                                        providerRef = "GASU",
                                        prepareTime = DateTime.Now,
                                        uid = Guid.NewGuid().ToString(),
                                        Series = new GASU.SeriesType[] 
                                        {
                                            new GASU.SeriesType
                                            {
                                                SeriesKey = new GASU.SeriesTypeSeriesKeyItem[] 
                                                {
                                                    new GASU.SeriesTypeSeriesKeyItem 
                                                    {
                                                        DimensionRef = "SP1",
                                                        Value = "0300"
                                                    },
                                                    new GASU.SeriesTypeSeriesKeyItem 
                                                    {
                                                        DimensionRef="SP_ZPMUP",
                                                        Value = "1"
                                                    }
                                                },
                                                Observation = new GASU.SeriesTypeObservation 
                                                {
                                                    Time = "2014",
                                                    ObsValue = new GASU.ObsValueType
                                                    {
                                                        ValueVc = 447,
                                                        ValueVcSpecified = true,
                                                        ValueVo = 0,
                                                        ValueVoSpecified = true,
                                                        ValueVs = 0,
                                                        ValueVsSpecified = true
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    
                                }
                            }
                        }
                    }
                }
            };
            #endregion
            try
            {
                var result = client.publish(msg);
            }
            catch (Exception e)
            {   
            }

        }
    }
}
