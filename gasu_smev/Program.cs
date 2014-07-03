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
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var client = new GASU.gasu2Client();
            client.ClientCredentials.ClientCertificate.Certificate = store.Certificates[0];
            client.ClientCredentials.ServiceCertificate.DefaultCertificate = store.Certificates[0];

            client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            client.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            string serverCommonName = store.Certificates[0].GetNameInfo(X509NameType.SimpleName, false);
            EndpointAddress correctEPAddress = new EndpointAddress(new Uri("http://188.254.16.92:7777/gateway/services/SID0003565?wsdl"),
                                                                EndpointIdentity.CreateDnsIdentity(
                                                                serverCommonName));
            client.Endpoint.Address = correctEPAddress;


            CustomBinding binding = new CustomBinding(client.Endpoint.Binding);
            SMEVMessageEncodingBindingElement textBindingElement = new SMEVMessageEncodingBindingElement()
            {
                MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap11, AddressingVersion.None)
            };

            binding.Elements.Remove<TextMessageEncodingBindingElement>();
            binding.Elements.Insert(0, textBindingElement);
            /// Не включаем метку времени в заголовок Security
            binding.Elements.Find<AsymmetricSecurityBindingElement>().IncludeTimestamp = false;
            /// Говорим WCF, что в сообщении от СМЭВ не нужно искать метку времени и nonce.
            binding.Elements.Find<AsymmetricSecurityBindingElement>().LocalClientSettings.DetectReplays = false;

            /// Устанавливаем модифицированную привязку.
            client.Endpoint.Binding = binding;

            client.ChannelFactory.Endpoint.Contract.ProtectionLevel = ProtectionLevel.None;
            #region MSG
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

            client.publish(msg);
        }
    }
}
