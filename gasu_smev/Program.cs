using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace gasu_smev
{
    class Program
    {
        static void Main(string[] args)
        {
            GASU.GasuMessage msg = new GASU.GasuMessage();

            //msg.Message = new GASU.MessageType();
            //msg.Message.Date = DateTime.Now;
            //msg.Message.TypeCode = GASU.TypeCodeType.GSRV;

            //msg.Message.Originator = new GASU.orgExternalType();
            //msg.Message.Originator.Code = "MINECONOMSK_SYS_1";
            //msg.Message.Originator.Name = "Минэкономразвития СК";

            //msg.Message.Recipient = new GASU.orgExternalType();
            //msg.Message.Recipient.Code = "13312";
            //msg.Message.Recipient.Name = "ФНС";

            //msg.Message.Sender = new GASU.orgExternalType();
            //msg.Message.Sender.Code = "MINECONOMSK_SYS_1";
            //msg.Message.Sender.Name = "Минэкономразвития СК";

            //msg.MessageData = new GASU.GasuMessageMessageData();
            //msg.MessageData.AppData = new GASU.GasuMessageMessageDataAppData();
            //msg.MessageData.AppData.AppMessage = new GASU.AppMessageType();
            //msg.MessageData.AppData.AppMessage.MessageType = GASU.MessageTypeType.ImportFull;
            //msg.MessageData.AppData.AppMessage.AppHeader = new GASU.AppHeaderType();
            //msg.MessageData.AppData.AppMessage.AppHeader.DataSourceRef = "8765";
            //msg.MessageData.AppData.AppMessage.AppHeader.ID = "12345";
            //msg.MessageData.AppData.AppMessage.AppHeader.HeaderInfo = new GASU.AppDataType();

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certificate = store.Certificates[0];

            GASU.gasu2Client client = new GASU.gasu2Client();

            client.ClientCredentials.ClientCertificate.Certificate = certificate;
            client.ClientCredentials.ServiceCertificate.DefaultCertificate = certificate;

            client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            client.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            string serverCommonName = certificate.GetNameInfo(X509NameType.SimpleName, false);

            EndpointAddress myEP = new EndpointAddress(new Uri(@"http://188.254.16.92:7777/gateway/services/SID0003565?wsdl"),
                EndpointIdentity.CreateDnsIdentity(serverCommonName));

            client.Endpoint.Address = myEP;

            client.ChannelFactory.Endpoint.Contract.ProtectionLevel = ProtectionLevel.Sign;

            var result = client.publish(msg);
        }
    }
}
