using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Receiver
{
    class Receiver
        {
        public static int SEQ = 20191;
        public static int SYN = 1;
        public static int ACK = 0;
        public static int MSS = 576;
        public static int SEGM = 1;
        public static float NUM_SEGM = 5; // Default
        public static int temp = 0;
        public static bool retras = false;
        public static int tryRetras = 5; // Number retransmissions
        public static int counter;
        public static int lastBytes = 0;

        static void Main(string[] args)
        {
            string optn         = Menu();
            int ctn             = 2;
            Socket s            = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");
            UdpClient listener  = new UdpClient(11000);
            IPEndPoint groupEP  = new IPEndPoint(IPAddress.Any, 11000);
            // Path to save file
            string path         = PathSaveFile();

            /**
              * CTN = 0: Close connection  
              * CTN = 1: Reset and reconection with new file
              * CTN = 2: Connection  
            **/

            switch (optn)
            {
                case "1":
                    while (SEGM < (NUM_SEGM) || ctn > 0)
                    {
                        // Reset connection
                        if (ctn == 1)
                        {
                            Console.Clear();
                            ResetConnection();
                        }

                        if (ctn == 1 && tryRetras < 0)
                        {
                            ResetConnection();
                        }
                        else if (tryRetras == 0)
                        {
                            break;
                        }

                        // Open connection
                        ctn = EstablishConnection(listener, groupEP, path);

                        // Receive data
                        if (ctn == 2)
                        {
                            ctn = SendResponse(s, broadcast);
                        }
                    }
                    break;
                case "0":
                    Console.WriteLine("Thank you, see you again!");
                    break;
                default:
                    Console.WriteLine("Thank you!");
                    break;
            }

            Console.WriteLine("Press ENTER to close!");
            Console.ReadLine();
        }

        static string Menu()
        {
            string optn;

            Console.WriteLine("*************************");
            Console.WriteLine("MENU");
            Console.WriteLine("1 - Receive data");
            Console.WriteLine("0 - Exit");
            Console.WriteLine("*************************");
            optn = Console.ReadLine();

            return optn;
        }

        static int EstablishConnection(UdpClient listener, IPEndPoint groupEP, String path)
        {
            Console.WriteLine("Waiting data...");
            listener.Client.ReceiveTimeout = 5000;
            int percent                    = 0;

            try
            {
                byte[] bytes   = listener.Receive(ref groupEP);
                string content = Encoding.Default.GetString(bytes);
                string[] info  = content.Split('_');
                bool retry     = RetryPacket(info, SEGM); 

                // Increase counter if not retransmit
                if (!retry) {
                    counter++; // Number iterations
                }

                if (SYN == 1)
                {
                    if (MSS >= int.Parse(info[3]))
                    {
                        MSS = int.Parse(info[3]);
                    }
                }
                else
                {
                    MSS = 0;
                }

                SYN  = int.Parse(info[0]);
                ACK  = int.Parse(info[1]) + 1;
                SEGM = int.Parse(info[4]) + 1;

                if (SYN == 0 && temp >= 1)
                {
                    SEQ       = int.Parse(info[2]);
                    ACK       = int.Parse(info[1]) + int.Parse(info[(info.Length-2)]); // info[8], because enters
                    lastBytes = int.Parse(info[(info.Length-2)]);
                }

                // Force error to retransmit
                // if(temp >= 2)
                // {
                //     SEQ       = int.Parse(info[2]);
                //     ACK       = int.Parse(info[1]); // Error;
                //     lastBytes = int.Parse(info[(info.Length-2)]);
                // }

                // Update num segments
                NUM_SEGM = int.Parse(info[5]);
                
                percent = (int)(((576 * counter) * 100) / (NUM_SEGM * 576) < 100 ? ((counter * 576) * 100) / (NUM_SEGM * 576) : 100) * 2;
                
                Console.WriteLine("Overall: {0}%", percent);

                if (percent <= 100 && SYN == 0) 
                {
                    using (var stream = new FileStream(path + info[6], FileMode.Append))
                    {
                        stream.Write(Encoding.Default.GetBytes(info[7]), 0, Encoding.Default.GetBytes(info[7]).Length);
                    };
                }

                // Show status
                Console.WriteLine("***********");
                Console.WriteLine("SEGMENT: {0}", SEGM);
                Console.WriteLine("MSS: {0}", MSS);
                Console.WriteLine("SYN: {0}", SYN);
                Console.WriteLine("SEQ: {0}", SEQ);
                Console.WriteLine("ACK: {0}", ACK);
                Console.WriteLine("***********");

                return 2;
            }
            catch (SocketException e)
            {
                retras = true;
                tryRetras--;
                Console.WriteLine("Error, on response! Available attempts: {0}", tryRetras);
                return 0;
            }
        }

        static int SendResponse(Socket s, IPAddress broadcast)
        {
            string content = SYN.ToString() + '_' + SEQ.ToString() + 
                '_' + ACK.ToString() + '_' + MSS.ToString() + 
                '_' + SEGM.ToString() + '_' + SEGM.ToString() + '_' + lastBytes.ToString();

            try
            {
                byte[] sendbuf = Encoding.ASCII.GetBytes(content);
                IPEndPoint ep  = new IPEndPoint(broadcast, 11001);
                s.SendTo(sendbuf, ep);
                Console.WriteLine("Message sent!");
                temp++;
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }

            if (SEGM >= (NUM_SEGM))
            {
                Console.WriteLine("Do you want to stay listen?");
                Console.WriteLine("1 - Yes");
                Console.WriteLine("0 - No");
                string resp = Console.ReadLine();

                return (int.Parse(resp) == 1 ? 1 : 0);
            }

            return 2;
        }

        static void ResetConnection()
        {
            SEQ        = 20191;
            SYN        = 1;
            ACK        = 0;
            MSS        = 8192;
            SEGM       = 1;
            temp       = 0;
            retras     = false;
            tryRetras  = 5;
            counter    = 0;
            lastBytes  = 0;
        }

        static bool RetryPacket(string[] info, int segm)
        {
            // Same current segm is retransmiting
            if(int.Parse(info[4]) + 1 == segm)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static string PathSaveFile()
        {
            string path = null;
            Console.Write("Specify the file path to save file: ");
            path      = Console.ReadLine();
            bool fileExists  = false;

            do
            {
                // If path exists
                if (Directory.Exists(path))
                {
                    fileExists = true;
                }
                else
                {
                    Console.WriteLine("Path does not exist. Please try again.");
                    path = Console.ReadLine(); // Ask again
                }
            } while (!fileExists); // While path doesn't exists

            // Check SO
            if (System.Environment.OSVersion.Platform.Equals(System.PlatformID.Unix)) {
                Regex regexUnix = new Regex(@"^\/\d|\D+\/$"); // Check last char is '/'
                Match matchUnix = regexUnix.Match(path);

                // No match unix path
                if (!matchUnix.Success) { 
                    path = path + "/";
                } 
            }
            else {
                Regex regexWin = new Regex(@"^(?:\w\:|[\w\.]+\\[\w.$]+)\\(?:[\w]+\\)+\w+\\$"); // Check last char is '\'
                Match matchWin = regexWin.Match(path);

                // No match windows path
                if (!matchWin.Success) {
                    path = path + "\\";
                }
            }

            return path;
        }
    }
}
