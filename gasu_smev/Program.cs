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

namespace gasu_smev
{
    class Program
    {
        static void Main(string[] args)
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

            var client = new GASU.gasu2Client();
            client.ClientCredentials.ClientCertificate.Certificate = certs[0];
            client.ClientCredentials.ServiceCertificate.DefaultCertificate = certs[0];

            client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            client.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            string serverCommonName = certs[0].GetNameInfo(X509NameType.SimpleName, false);
            EndpointAddress correctEPAddress = new EndpointAddress(new Uri("http://smev-mvf.test.gosuslugi.ru:7777/gateway/services/SID0003565?wsdl"),
                                                                EndpointIdentity.CreateDnsIdentity(
                                                                serverCommonName));
            client.Endpoint.Address = correctEPAddress;


            //CustomBinding binding = new CustomBinding(client.Endpoint.Binding);
            //SMEVMessageEncodingBindingElement textBindingElement = new SMEVMessageEncodingBindingElement()
            //{
            //    MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap11, AddressingVersion.None),
            //    SenderActor = "http://smev.gosuslugi.ru/actors/recipient",
            //    RecipientActor = "http://smev.gosuslugi.ru/actors/smev"
            //};

            //binding.Elements.Remove<TextMessageEncodingBindingElement>();
            //binding.Elements.Insert(0, textBindingElement);
            ///// Не включаем метку времени в заголовок Security
            //binding.Elements.Find<AsymmetricSecurityBindingElement>().IncludeTimestamp = false;
            ///// Говорим WCF, что в сообщении от СМЭВ не нужно искать метку времени и nonce.
            //binding.Elements.Find<AsymmetricSecurityBindingElement>().LocalClientSettings.DetectReplays = false;

            ///// Устанавливаем модифицированную привязку.
            //client.Endpoint.Binding = binding;


            var msgId = Guid.NewGuid().ToString();

            var datetime = string.Format("{0:yy-mm-ddTHH:mm:ss.Ms}+04:00", DateTime.Now);
            var dateTime2 = string.Format("{0:yy-mm-ddTHH:mm:ss}", DateTime.Now);

            client.ChannelFactory.Endpoint.Contract.ProtectionLevel = ProtectionLevel.Sign;


            Publish(client, msgId);
           // Query(client, msgId);
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
                        Code = "DISKK01",
                        Name = "Автоматизированная информационная система \"Мониторинг социально-экономического развития Краснодарского края\""
                    },
                    Recipient = new GASU.orgExternalType
                    {
                        Code = "DISKK01",
                        Name = "Автоматизированная информационная система \"Мониторинг социально-экономического развития Краснодарского края\""
                    },
                    Originator = new GASU.orgExternalType
                    {
                        Code = "DISKK01",
                        Name = "Автоматизированная информационная система \"Мониторинг социально-экономического развития Краснодарского края\""
                    },
                    ServiceName = "",
                    TypeCode = GASU.TypeCodeType.GFNC,
                    Status = GASU.StatusType.REQUEST,
                    Date = DateTime.Now,
                    ExchangeType = "", // compleate!!
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
                                DataSourceRef = "DISKK01",
                                ID = "DISKK01",
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
                        Code = "DISKK01",
                        Name = "Автоматизированная информационная система \"Мониторинг социально-экономического развития Краснодарского края\""
                    },
                    Recipient = new GASU.orgExternalType
                    {
                        Code = "DISKK01",
                        Name = "Автоматизированная информационная система \"Мониторинг социально-экономического развития Краснодарского края\""
                    },
                    Originator = new GASU.orgExternalType
                    {
                        Code = "DISKK01",
                        Name = "Автоматизированная информационная система \"Мониторинг социально-экономического развития Краснодарского края\""
                    },
                    ServiceName = "",
                    TypeCode = GASU.TypeCodeType.GFNC,
                    Status = GASU.StatusType.REQUEST,
                    Date = DateTime.Now,
                    ExchangeType = "", // compleate!!
                    RequestIdRef = msgId,
                    OriginRequestIdRef = msgId,
                    ServiceCode = "",
                    CaseNumber = "",
                    TestMsg = ""
                },
                MessageData = new GASU.GasuMessageMessageData
                {
                    AppData = new GASU.GasuMessageMessageDataAppData
                    {
                        AppMessage = new GASU.AppMessageType
                        {
                            AppHeader = new GASU.AppHeaderType
                            {
                                ID = "DISKK01",
                                DataSourceRef = "DISKK01",
                                HeaderInfo = new GASU.AppDataType()
                            },
                            MessageType = GASU.MessageTypeType.ImportFull,
                            Body = new GASU.AppMessageTypeBody
                            {
                                Items = new GASU.DataSetType[] 
                                {
                                    new GASU.DataSetType 
                                    {
                                        indicatorRef = "ГАСУ/МУП/Р/1",
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
                                                        Value = "3"
                                                    }
                                                },
                                                Observation = new GASU.SeriesTypeObservation 
                                                {
                                                    Time = "2014",
                                                    ObsValue = new GASU.ObsValueType
                                                    {
                                                        ValueVc = 3,
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
                                    //new GASU.DataSetType 
                                    //{
                                    //    indicatorRef = "ГАСУ/МУП/Р/2",
                                    //    providerRef = "GASU",
                                    //    prepareTime = DateTime.Now,
                                    //    uid = Guid.NewGuid().ToString(),
                                    //    Series = new GASU.SeriesType[] 
                                    //    {
                                    //        new GASU.SeriesType
                                    //        {
                                    //            SeriesKey = new GASU.SeriesTypeSeriesKeyItem[] 
                                    //            {
                                    //                new GASU.SeriesTypeSeriesKeyItem 
                                    //                {
                                    //                    DimensionRef = "SP1",
                                    //                    Value = "0300"
                                    //                }
                                    //            },
                                    //            Observation = new GASU.SeriesTypeObservation 
                                    //            {
                                    //                Time = "2014-05",
                                    //                ObsValue = new GASU.ObsValueType
                                    //                {
                                    //                    ValueVc = 24.5d,
                                    //                    ValueVcSpecified = true
                                    //                }
                                    //            }
                                    //        }
                                    //    }
                                    //}
                                }
                            }
                        }
                    }
                }
            };

            client.publish(msg);
            #endregion
        }
    }
}
