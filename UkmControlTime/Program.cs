using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace UkmControlTime
{
    class Program
    {
        private static void Main(string[] args)
        {
            Color.WriteLineColor("Программа запущена!",ConsoleColor.Green);

            prg();

            Console.ReadKey();
        }

        private static void prg()
        {
            if (!IsTimeStart())
            {
                if (CheckLocalTime())
                {
                    StartCheck();
                }
                else
                    ChangeTime(GetNTPTime());
            }

            CheckTimeToStart();

        }

        static void ChangeTime(DateTime time)
        {
            System.Diagnostics.Process timeChanger = System.Diagnostics.Process.Start("cmd.exe", "/c time" + time);
        }

        private static bool IsTimeStart()
        {
            if (DateTime.Now.TimeOfDay.IsBetween(new TimeSpan(7, 0, 0), new TimeSpan(8, 0, 0)))
                return true;
            else
                return false;
        }

        public static void CheckTimeToStart()
        {
            TimeSpan diff = new TimeSpan();

            DateTime TimeNow = DateTime.Now;
            DateTime TimeEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 07, 00, 01);

            if (TimeNow.Hour > 7 && TimeNow.Hour < 24)
            {
                TimeEnd = TimeEnd.AddDays(1);
            }

            diff = TimeEnd - TimeNow;

            Thread thd = new Thread(delegate()
            {
                Console.WriteLine("\n");
                Color.WriteLineColor("Запускаю поток контролера", ConsoleColor.Green);
                WaitTime(diff);
            });
            thd.Name = "Поток контролера";
            thd.Start();
        }

        static void WaitTime(TimeSpan diff)
        {
            int i = Convert.ToInt32(diff.TotalSeconds);
            string text;

            ConsoleColor cl = new ConsoleColor();

            text = "До активации планировщика:           ";
            cl = ConsoleColor.Green;

            Console.WriteLine("\n");

            while (i != 0)
            {
                int hour = i / 3600;
                int minute = (i / 60) - (hour * 60);
                int second = i - (minute * 60) - (hour * 3600);
                string n = "";
                string m = "";


                if (second < 10)
                    n = "0";

                if (minute < 10)
                    m = "0";

                Code.RenderConsoleProgress(0, '\u2592', cl, text + hour + ":" + m + minute + ":" + n + second);
                Thread.Sleep(1000);
                i--;

                n = "";
                m = "";
            }

            prg();
        }

        private static bool CheckLocalTime()
        {
            DateTime ntp = GetNTPTime().ToLocalTime();
            DateTime local = DateTime.Now;
            Color.WriteLineColor("Время в интернете: " + ntp, ConsoleColor.Green);
            Color.WriteLineColor("Время на сервере: " + local, ConsoleColor.Green);

            if (ntp > local)
                Color.WriteLineColor("Часы отстают на : " + (ntp - local),ConsoleColor.Yellow);
            else
                Color.WriteLineColor("Часы спешат на : " + (local - ntp), ConsoleColor.Yellow);

            if ((ntp - local).TotalSeconds > 60)
            {
                Color.WriteLineColor("Разница больше 60 секунд!", ConsoleColor.Red);
                return false;
            }
            else
            {
                Color.WriteLineColor("Разница в пределах нормы!", ConsoleColor.Green);
                return true;
            }
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

                IPAddress[] addressList = Dns.GetHostEntry("time.windows.com").AddressList;

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
            
            IPs.Add("192.168.1.160");

            Ping Pinger = new Ping();
            
            foreach (string ip in IPs)
            {
                try
                {
                    PingReply Reply = Pinger.Send(ip);
                    Color.WriteLineColor("Ip: " + ip + " Статус: " + Reply.Status.ToString(),ConsoleColor.Green);

                    if (Reply.Status == IPStatus.Success)
                    {
                        System.Diagnostics.Process.Start(Environment.CurrentDirectory + "\\script\\host.bat " + ip);
                    }
                    try
                    {
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = Environment.CurrentDirectory + "\\Script\\host.cmd";
                        p.StartInfo.Arguments = ip;
                        p.Start();
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    bool status = true;

                    while (status)
                    {
                        Thread.Sleep(1000);

                        if (File.Exists(Environment.CurrentDirectory + "\\script\\error.flg"))
                        {
                            Color.WriteLineColor("Установить время на терминале "+ ip +" не удалось !", ConsoleColor.Red);
                            File.Delete(Environment.CurrentDirectory + "\\script\\error.flg");
                            status = false;
                        }

                        if (File.Exists(Environment.CurrentDirectory + "\\script\\success.flg"))
                        {
                            Color.WriteLineColor("Операция завершена успешно!", ConsoleColor.Green);
                            File.Delete(Environment.CurrentDirectory + "\\script\\success.flg");
                            status = false;
                        }
                    }

                    Color.WriteLineColor("Перехожу к следующей операции", ConsoleColor.Cyan);

                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
