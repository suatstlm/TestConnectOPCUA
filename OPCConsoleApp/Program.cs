using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace TestConnectOPCUA_Console
{
    class Program
    {
        private static Session m_session;
        private static Subscription m_subscription;

        static void Main(string[] args)
        {
            Console.WriteLine("OPC UA Client Starting with .NET 8...");

            string serverUrl = "opc.tcp://servertest:49320";
            string appName = "testopcua";

            var config = CreateApplicationConfiguration(appName);
            config.Validate(ApplicationType.Client).Wait();

            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) =>
                {
                    e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
                };
            }

            var application = new ApplicationInstance
            {
                ApplicationName = appName,
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config
            };

            application.CheckApplicationInstanceCertificate(true, 2048).Wait();

            Console.WriteLine("Connecting to server: " + serverUrl);

            m_session = Session.Create(config, new ConfiguredEndpoint(null, new EndpointDescription(serverUrl)), false, false, appName, 60000, null, null).Result;

            Console.WriteLine("Connected to server.");

            CreateSubscriptionAndMonitorItem();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();

            m_session.CloseAsync().Wait();
        }

        private static ApplicationConfiguration CreateApplicationConfiguration(string appName)
        {
            return new ApplicationConfiguration
            {
                ApplicationName = appName,
                ApplicationUri = Utils.Format("urn:{0}:" + appName, System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = AppContext.BaseDirectory + "Cert\\TrustedIssuer",
                        SubjectName = "CN=" + appName + ", DC=" + System.Net.Dns.GetHostName()
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = AppContext.BaseDirectory + "Cert\\TrustedIssuer"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = AppContext.BaseDirectory + "Cert\\TrustedIssuer"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = AppContext.BaseDirectory + "Cert\\RejectedCertificates"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true,
                    RejectSHA1SignedCertificates = false
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
        }

        private static void CreateSubscriptionAndMonitorItem()
        {
            try
            {
                if (m_session == null)
                {
                    Console.WriteLine("Session is not initialized.");
                    return;
                }

                m_subscription = new Subscription(m_session.DefaultSubscription)
                {
                    PublishingEnabled = true,
                    PublishingInterval = 1000,
                    Priority = 1,
                    KeepAliveCount = 10,
                    LifetimeCount = 20,
                    MaxNotificationsPerPublish = 1000
                };

                m_session.AddSubscription(m_subscription);
                m_subscription.Create();

                var monitoredItems = new List<string>
                {
                    "Simulation Examples.Functions.Random3",
                    "Simulation Examples.Functions.Random4",
                    "Simulation Examples.Functions.Random2",
                    "Channel2.Device1.v2"
                };

                foreach (var tag in monitoredItems)
                {
                    var monitoredItem = new MonitoredItem(m_subscription.DefaultItem)
                    {
                        StartNodeId = new NodeId(tag, 2),
                        AttributeId = Attributes.Value,
                        Notification += MonitoredItem_Notification
                    };

                    m_subscription.AddItem(monitoredItem);
                }

                m_subscription.ApplyChanges();

                Console.WriteLine("Monitoring started for tags:");
                monitoredItems.ForEach(tag => Console.WriteLine($" - {tag}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating subscription: {ex.Message}");
            }
        }

        private static void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                var notification = e.NotificationValue as MonitoredItemNotification;
                if (notification != null)
                {
                    Console.WriteLine($"Tag: {monitoredItem.StartNodeId}, Value: {notification.Value.WrappedValue}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in monitored item notification: {ex.Message}");
            }
        }
    }
}
