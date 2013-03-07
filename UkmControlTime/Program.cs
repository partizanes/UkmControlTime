using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace UkmControlTime
{
    class Program
    {
        private static void Main(string[] args)
        {
            Console.ReadKey();
            //StartCheck();
        }

        private static bool CheckLocalTime()
        {
            DateTime ntp = GetNTPTime().ToLocalTime();
            DateTime local = DateTime.Now;
            Console.WriteLine("Время в интернете: " + ntp);
            Console.WriteLine("Время на сервере: " + local);

            if (ntp > local)
            {
                Console.WriteLine("Часы отстают на : " + (ntp - local));
            }
            else
            {
                Console.WriteLine("Часы спешат на : " + (local - ntp));
            }

            return true;
        }

        public static DateTime GetNTPTime()
        {
            try
            {
                // 0x1B == 0b11011 == NTP version 3, client - see RFC 2030
                byte[] ntpPacket = new byte[] { 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                IPAddress[] addressList = Dns.GetHostEntry("pool.ntp.org").AddressList;

                if (addressList.Length == 0)
                {
                    // error
                    return DateTime.MinValue;
                }

                IPEndPoint ep = new IPEndPoint(addressList[0], 123);
                UdpClient client = new UdpClient();
                client.Connect(ep);
                client.Send(ntpPacket, ntpPacket.Length);
                byte[] data = client.Receive(ref ep);

                // receive date data is at offset 32
                // Data is 64 bits - first 32 is seconds - we'll toss the fraction of a second
                // it is not in an endian order, so we must rearrange
                byte[] endianSeconds = new byte[4];
                endianSeconds[0] = (byte)(data[32 + 3] & (byte)0x7F); // turn off MSB (some servers set it)
                endianSeconds[1] = data[32 + 2];
                endianSeconds[2] = data[32 + 1];
                endianSeconds[3] = data[32 + 0];
                uint seconds = BitConverter.ToUInt32(endianSeconds, 0);

                return (new DateTime(1900, 1, 1)).AddSeconds(seconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (new DateTime(1900, 1, 1));
            }
        }

        private static void StartCheck()
        {
            List<string> IPs = new List<string>();
            
            IPs.Add("192.168.1.150");
            IPs.Add("192.168.1.151");
            IPs.Add("192.168.1.152");
            IPs.Add("192.168.1.153");
            IPs.Add("192.168.1.154");
            IPs.Add("192.168.1.155");
            IPs.Add("192.168.1.156");
            IPs.Add("192.168.1.157");
            IPs.Add("192.168.1.158");
            IPs.Add("192.168.1.159");
            IPs.Add("192.168.1.160");

            Ping Pinger = new Ping();
            foreach (string ip in IPs)
            {
                try
                {
                    PingReply Reply = Pinger.Send(ip);
                    Console.WriteLine("Ping " + ip + ": " + Reply.Status.ToString());
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
