using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;
using WakeMe.Models;

namespace WakeMe.Interfaces
{
    public interface IWakeOnLan
    {
        void Wake(string macAddress);
    }
    public static class IPAddressExtensions
    {
        public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }

        public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
        {
            IPAddress network1 = address.GetNetworkAddress(subnetMask);
            IPAddress network2 = address2.GetNetworkAddress(subnetMask);

            return network1.Equals(network2);
        }
    }
    public class WakeOnLanProvider : IWakeOnLan
    {
        public void Wake(string macAddress)
        {
            var client = new UdpClient();

            var datagram = new byte[102];

            for (var i = 0; i <= 5; i++)
            {
                datagram[i] = 0xff;
            }

            var macDigits = macAddress.Split(macAddress.Contains("-") ? '-' : ':');

            if (macDigits.Length != 6)
            {
                throw new ArgumentException("Incorrect MAC address supplied!");
            }

            var start = 6;
            for (var i = 0; i < 16; i++)
            {
                for (var x = 0; x < 6; x++)
                {
                    datagram[start + i * 6 + x] = (byte)Convert.ToInt32(macDigits[x], 16);
                }
            }

            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    foreach (var address in  @interface.GetIPProperties().UnicastAddresses.ToList())
                    {
                        if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            IPAddress mask = address.IPv4Mask;
                            IPAddress broadcastAddress = address.Address.GetBroadcastAddress(mask);

                            client.EnableBroadcast = true;
                            client.Send(datagram, datagram.Length, broadcastAddress.ToString(), 3);
                            client.Send(datagram, datagram.Length, broadcastAddress.ToString(), 6);
                            client.Send(datagram, datagram.Length, broadcastAddress.ToString(), 7);
                            client.Send(datagram, datagram.Length, broadcastAddress.ToString(), 9);
                        }
                    }
                }
                catch
                {
                    // Ignored.
                }
            }
        }
    }

    public interface IItemStorage
    {
        IQueryable<WakeOnLanEntry> GetEntries();
        void Update(WakeOnLanEntry entry);
        void Create(WakeOnLanEntry entry);
        void Delete(WakeOnLanEntry entry);
        WakeOnLanEntry Find(string id);
        void Save();
    }

    public class JsonItemStorage : IItemStorage
    {
        private string BasePath => HostingEnvironment.MapPath("~/App_Data");
        private string FileName => Path.Combine(BasePath, "items.json");

        private List<WakeOnLanEntry> _items;
        private List<WakeOnLanEntry> Items => _items ?? (_items = Load());

        private List<WakeOnLanEntry> Load()
        {
            try
            {
                using (var fileStream = File.Open(FileName, FileMode.Open, FileAccess.Read))
                {
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        var serializer = new JsonSerializer();
                        using (var jsonReader = new JsonTextReader(streamReader))
                        {
                            return serializer.Deserialize<List<WakeOnLanEntry>>(jsonReader);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                return new List<WakeOnLanEntry>();
            }
        }

        public void Save()
        {
            Save(Items);
        }

        private void Save(List<WakeOnLanEntry> items)
        {
            using (var fileStream = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var streamReader = new StreamWriter(fileStream))
                {
                    var serializer = new JsonSerializer();
                    using (var jsonReader = new JsonTextWriter(streamReader))
                    {
                        serializer.Serialize(jsonReader, items);
                    }
                }
            }
        }

        public IQueryable<WakeOnLanEntry> GetEntries()
        {
            return Items.ToArray().AsQueryable();
        }

        public void Update(WakeOnLanEntry entry)
        {
            var existing = Items.SingleOrDefault(i => i.Id == entry.Id);
            if (existing == null) throw new InvalidOperationException($"Entry: {entry.Id} was not found.");
            existing.MacAddress = entry.MacAddress;
            existing.Name = entry.Name;
        }

        public void Create(WakeOnLanEntry entry)
        {
            if (Items.Any(i => i.Id == entry.Id))
            {
                throw new InvalidOperationException("That entry already exists.");
            }
            Items.Add(entry);
        }

        public void Delete(WakeOnLanEntry entry)
        {
            Items.RemoveAll(i => i.Id == entry.Id);
        }

        public WakeOnLanEntry Find(string id)
        {
            return Items.SingleOrDefault(i => i.Id == id);
        }
    }
}