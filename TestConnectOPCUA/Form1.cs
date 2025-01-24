using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestConnectOPCUA
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private Session m_session;
        private bool m_connectedOnce;
        private Subscription m_subscription;
        private void connectServerCtrl1_ConnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = connectServerCtrl1.Session;

                // set a suitable initial state.
                if (m_session != null && !m_connectedOnce)
                {
                    m_connectedOnce = true;
                    //connected
                    CreateSubscriptionAndMonitorItem();
                }


            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        private void CreateSubscriptionAndMonitorItem()
        {
            try
            {
                if (m_session == null)
                {
                    return;
                }

                if (m_subscription != null)
                {
                    m_session.RemoveSubscription(m_subscription);
                    m_subscription = null;
                }

                m_subscription = new Subscription();
                m_subscription.PublishingEnabled = true;
                m_subscription.PublishingInterval = 1000;
                m_subscription.Priority = 1;
                m_subscription.KeepAliveCount = 10;
                m_subscription.LifetimeCount = 20;
                m_subscription.MaxNotificationsPerPublish = 1000;

                m_session.AddSubscription(m_subscription);
                m_subscription.Create();


                //Method 1
                string[] tags = new string[]
                {
                    "Simulation Examples.Functions.Random3",
                    "Simulation Examples.Functions.Random4"
                };



                Control[] ctrls = new Control[]
                {
                    label1,
                    label2
                };

                for (int ii = 0; ii < tags.Length; ii++)
                {
                    ctrls[ii].Text = "---";
                    MonitoredItem monitoredItem = new MonitoredItem();
                    monitoredItem.StartNodeId = new NodeId(tags[ii], 2);
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.Handle = new object[] { ctrls[ii], ctrls[ii].GetType().GetProperty("Text") };
                    monitoredItem.Notification += MonitoredItem_Notification1;
                    m_subscription.AddItem(monitoredItem);

                }
                //Method 2:
                this.textBox1.Text = "---";
                MonitoredItem monitoredItem2 = new MonitoredItem();
                monitoredItem2.StartNodeId = new NodeId("Simulation Examples.Functions.Random2", 2);
                monitoredItem2.AttributeId = Attributes.Value;
                monitoredItem2.Notification += MonitoredItem2_Notification;
                m_subscription.AddItem(monitoredItem2);
                //Method 3:

                MonitoredItem monitoredItem3 = new MonitoredItem();
                monitoredItem3.StartNodeId = new NodeId("Channel2.Device1.v2", 2);
                monitoredItem3.AttributeId = Attributes.Value;
                monitoredItem3.Notification += MonitoredItem3_Notification;
                m_subscription.AddItem(monitoredItem3);

                m_subscription.ApplyChanges();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        private void MonitoredItem3_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MonitoredItemNotificationEventHandler(MonitoredItem3_Notification), monitoredItem, e);
                return;
            }
            try
            {
                if ((bool)((MonitoredItemNotification)e.NotificationValue).Value.WrappedValue.Value == false)
                {
                    this.pictureBox1.Image = global::TestConnectOPCUA.Properties.Resources.BlueButton;
                }
                else
                {
                    this.pictureBox1.Image = global::TestConnectOPCUA.Properties.Resources.BlackButtonPressed;
                }
            }
            catch (Exception ex)
            {
                ClientUtils.HandleException(this.Text, ex);
            }
        }

        private void MonitoredItem2_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MonitoredItemNotificationEventHandler(MonitoredItem2_Notification), monitoredItem, e);
                return;
            }
            this.textBox1.Text = ((MonitoredItemNotification)e.NotificationValue).Value.WrappedValue.ToString();

        }

        private void MonitoredItem_Notification1(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MonitoredItemNotificationEventHandler(MonitoredItem_Notification1), monitoredItem, e);
                return;
            }

            try
            {
                object[] objs = (object[])monitoredItem.Handle;
                Control control = (Control)objs[0];
                PropertyInfo proInfo = (PropertyInfo)objs[1];
                MonitoredItemNotification datachange = e.NotificationValue as MonitoredItemNotification;

                if (datachange == null)
                {
                    return;
                }
                object v = TypeDescriptor.GetConverter(datachange.Value.WrappedValue.Value.GetType()).ConvertTo(datachange.Value.WrappedValue, proInfo.PropertyType);
                if (proInfo != null) proInfo.SetValue(control, v);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.connectServerCtrl1.ServerUrl = "opc.tcp://servertest:49320";
            string AppName = "testopcua";//Issusedto
            ApplicationConfiguration config = new ApplicationConfiguration()
            {
                ApplicationName = AppName,
                ApplicationUri = Utils.Format(@"urn:{0}:" + AppName, System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = @"Directory",
                        StorePath = System.Windows.Forms.Application.StartupPath + @"\Cert\TrustedIssuer",
                        SubjectName = "CN=" + AppName + ", DC=" + System.Net.Dns.GetHostName()
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = @"Directory",
                        StorePath = System.Windows.Forms.Application.StartupPath + @"\Cert\TrustedIssuer"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = @"Directory",
                        StorePath = System.Windows.Forms.Application.StartupPath + @"\Cert\TrustedIssuer"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = @"Directory",
                        StorePath = System.Windows.Forms.Application.StartupPath + @"\Cert\RejectedCertificates"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true,
                    RejectSHA1SignedCertificates = false//important
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration
                {
                    DeleteOnLoad = true
                },
                DisableHiResClock = false

            };
            config.Validate(ApplicationType.Client).GetAwaiter().GetResult();
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, ee) =>
                { ee.Accept = (ee.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            }

            this.connectServerCtrl1.Configuration = config;
            //this.connectServerCtrl1.UserIdentity = new UserIdentity("tienttt", "123456"); //if you want to login with user and pass
            this.connectServerCtrl1.UserIdentity = new UserIdentity();
            this.connectServerCtrl1.UseSecurity = true;

            var application = new ApplicationInstance
            {
                ApplicationName = AppName,
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config

            };
            //set 0 Trace mask=>stop show log in output window.
            Opc.Ua.Utils.SetTraceMask(0);//
            application.CheckApplicationInstanceCertificate(true, 2048).GetAwaiter().GetResult();//create certificate

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.connectServerCtrl1.Disconnect();
        }
    }
}
