using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sender
{
    class Sender
    {
        public static int SEQ = 109876;
        public static int SYN = 1;
        public static int ACK = 0;
        public static int MSS = 576;
        public static int SEGM = 1;
        public static int temp = 0;
        public static int SIZE_FILE = 0;
        public static byte[] BUFFER_FILE;
        public static float NUM_SEGM = 4; // Default
        public static UdpClient listener = new UdpClient(11001);
        public static bool retras = false;
        public static int tryRetras = 5; // Number retransmissions
        public static bool isRetransmiting = false;
        public static int tryRetransmiting = 5;
        private static Random random = new Random();

        static void Main(string[] args)
        {
            string optn         = Menu();
            int ctn             = 2;
            Socket s            = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");
            IPEndPoint groupEP  = new IPEndPoint(IPAddress.Any, 11000);

            /**
              * CTN = 0: Close connection  
              * CTN = 1: Reset and reconection with new file
              * CTN = 2: Connection  
            **/

            switch (optn)
            {
                case "1":
                    while (SEGM < NUM_SEGM || ctn > 0)
                    {   
                        // Kill
                        if (ctn == -1) {
                            Console.WriteLine("Values not expected! (Attempts = 0)");
                            break;
                        }

                        // Reset connection
                        if (ctn == 1)
                        {
                            Console.Clear();
                            ResetConnection();
                        }

                        if (ctn == 0 && tryRetras < 0)
                        {
                            ResetConnection();
                        }
                        else if (tryRetras == 0)
                        {
                            break; // Timeout
                        }

                        // Open connection
                        string path = EstablishConnection(s, broadcast);
                        // Receive data
                        ctn = ReceiveResponse(groupEP);

                        // Send file
                        if (ctn == 2)
                        {
                            SendFile(s, broadcast, path, groupEP);
                        }
                    }
                    break;
                case "0":
                    Console.WriteLine("Thank you!");
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
            Console.WriteLine("1 - Establish Connection");
            Console.WriteLine("0 - Exit");
            Console.WriteLine("*************************");
            optn = Console.ReadLine();

            return optn;
        }

        static string EstablishConnection(Socket s, IPAddress broadcast)
        {
            string path = null;

            // Only first time
            if (SYN == 1 && retras == false)
            {
                Console.WriteLine("Specify the file path" + @" Example:(C:\Users\utilizador\Desktop\nomeFicheiro.extensao)");
                path = Console.ReadLine();
                FileRequest(path);
            }

            // Count number of required segments
            CountSegm();

            string content = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() + '_' + MSS.ToString() + '_' + SEGM.ToString() + '_' + NUM_SEGM.ToString();
            if (path != null)
            {
                try
                {
                    // Config send
                    byte[] sendbuf = Encoding.ASCII.GetBytes(content);
                    IPEndPoint ep  = new IPEndPoint(broadcast, 11000);

                    // Send content
                    s.SendTo(sendbuf, ep);
                    Console.WriteLine("Message sent!");
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
            }
            return path;
        }

        static void SendFile(Socket socket, IPAddress broadcast, string path, IPEndPoint groupEP)
        {
            byte[] buffer   = BUFFER_FILE;
            byte[] split    = new byte[576];
            int position    = 576;
            int counter     = 0; // Number iterations
            int percent     = 0;
            int leftBytes   = SIZE_FILE;
            string txt      = null;
            string fileName = Path.GetFileName(path);

            // Max allowed fileName size = 12
            if (fileName.ToString().Length > 12)
            {
                fileName = RandomString() + fileName.Substring(0, 8) + Path.GetExtension(path);
            }

            if (buffer.Length < 576)
            {
                txt = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() + 
                    '_' + MSS.ToString() + '_' + SEGM.ToString() + '_' + NUM_SEGM.ToString() + 
                    '_' + fileName + '_' + Encoding.Default.GetString(buffer) + 
                    '_' + (buffer.Length).ToString() + '_' + counter;
                    
                try
                {
                    byte[] sendbuf = Encoding.Default.GetBytes(txt);
                    IPEndPoint ep  = new IPEndPoint(broadcast, 11000);

                    // Send content
                    socket.SendTo(sendbuf, ep);

                    // Calculate percentage
                    percent = ((buffer.Length * 100) / SIZE_FILE < 100 ? (buffer.Length * 100) / SIZE_FILE : 100);

                    Console.WriteLine("Sending file...");
                    Console.WriteLine("Overall: {0}%", percent);

                    // Waiting response
                    if (percent < 100)
                    {
                        ReceiveResponse(groupEP); // Get ACK, SEQ...
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i += position)
                {

                    // Without attempts to retransmit
                    if (tryRetransmiting == 0) {
                        // Kill process
                        break;
                    }

                    leftBytes = SIZE_FILE - (counter * 576);
                    int bytesSended = counter * 576;
                    counter++; // Num iterations

                    if (leftBytes < 576)
                    {
                        if (leftBytes < 0)
                        {
                            leftBytes *= -1;
                        }
                        byte[] sp = new byte[leftBytes];
                        Array.Copy(buffer, i, sp, 0, leftBytes); // Copy position to array
                        txt = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() +
                            '_' + MSS.ToString() + '_' + SEGM.ToString() + '_' + NUM_SEGM.ToString() + 
                            '_' + fileName + '_' + Encoding.Default.GetString(sp) + '_' + leftBytes.ToString() + '_' + counter;
                    }
                    else
                    {
                        Array.Copy(buffer, i, split, 0, 576); // Copy position to array
                        txt = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() + '_' + MSS.ToString() + 
                            '_' + SEGM.ToString() + '_' + NUM_SEGM.ToString() + '_' + fileName + 
                            '_' + Encoding.Default.GetString(split) + '_' + position + '_' + counter;
                    }

                    try
                    {
                        byte[] sendbuf = Encoding.ASCII.GetBytes(txt);
                        IPEndPoint ep  = new IPEndPoint(broadcast, 11000);

                        if (isRetransmiting) {
                            i -= position;
                        }
                        else {
                            // Calculate percentage
                            percent = (((position * counter) * 100) / SIZE_FILE < 100 ? ((position * counter) * 100) / SIZE_FILE : 100);
                        }

                        // Send content
                        socket.SendTo(sendbuf, ep);

                        Console.WriteLine("Sending file...");
                        Console.WriteLine("Overall: {0}%", percent);

                        // Waiting response
                        if (percent < 100 && SYN == 0)
                        {
                            ReceiveResponse(groupEP); // Get ACK, SEQ...
                        }
                        else
                        {
                            break;
                        }

                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        static int ReceiveResponse(IPEndPoint groupEP)
        {
            if (tryRetransmiting == 0) { // Out of the loop
                // Kill process
                return -1;
            }

            Console.WriteLine("Waiting for data ...");
            listener.Client.ReceiveTimeout = 5000; // 5s timemout

            try
            {
                retras         = false;
                byte[] bytes   = listener.Receive(ref groupEP);
                string content = Encoding.ASCII.GetString(bytes);
                string[] info  = content.Split('_');

                // Show status
                Console.WriteLine("RECEIVED");
                Console.WriteLine("SEGMENT: {0}", SEGM + 1);
                Console.WriteLine("MSS: {0}", info[3]);
                Console.WriteLine("SYN: {0}", SYN);
                Console.WriteLine("SEQ: {0}", info[1]);
                Console.WriteLine("ACK: {0}", info[2]);
                Console.WriteLine("***********");

                // If ACK == (SEQ+1) and SYN_Receiver == 1 then connection granted
                if (int.Parse(info[2]) == (SEQ + 1) && int.Parse(info[0]) == 1)
                {
                    Console.WriteLine("Connection guaranteed!");
                    SYN = 0; MSS = 0;
                }
                Console.WriteLine("***********");

                bool retrySend = RetryPacket(info);

                if(retrySend && tryRetransmiting > 0) {
                    // Doesn't update data
                    return 2;
                }
                else if (tryRetransmiting == 0) { // Within the loop
                    // Kill process
                    return -1;
                }

                // If MSS >= than server MSS
                if (MSS >= int.Parse(info[3]))
                {
                    MSS = int.Parse(info[3]);
                }

                ACK  = int.Parse(info[1]) + 1;
                SEQ  = int.Parse(info[2]);
                SEGM = int.Parse(info[4]) + 1;

                if (SYN == 0 && temp >= 1)
                {
                    SEQ = int.Parse(info[2]) + int.Parse(info[6]);
                    ACK = int.Parse(info[1]) + 1;
                }
                temp++;
                
                // If number of SEGM > number of segments, file is sent in total.
                if (SEGM >= NUM_SEGM)
                {
                    Console.WriteLine("Do you want to send another file?");
                    Console.WriteLine("1 - Yes");
                    Console.WriteLine("0 - No");
                    string resp = Console.ReadLine();

                    return (int.Parse(resp) == 1 ? 1 : 0);
                }
                else
                {
                    // Show status
                    Console.WriteLine("SEGMENT: {0}", SEGM);
                    Console.WriteLine("MSS: {0}", MSS);
                    Console.WriteLine("SYN: {0}", SYN);
                    Console.WriteLine("SEQ: {0}", SEQ);
                    Console.WriteLine("ACK: {0}", ACK);
                    Console.WriteLine("***********");
                }

                return 2;

            }
            catch (Exception e)
            {
                retras = true;
                tryRetras--;
                Console.WriteLine("Error, on response! Available attempts: {0}", tryRetras);
                return 0;
            }
        }

        static void FileRequest(string path)
        {
            byte[] sizeFile  = null;
            bool allowedSize = false;
            bool fileExists  = false;

            do
            {
                // If path exists
                if (File.Exists(path))
                {
                    fileExists = true;
                    sizeFile   = File.ReadAllBytes(path); // Read all bytes

                    // Max 5MB
                    if (sizeFile.Length < 5242880)
                    {
                        allowedSize = true;
                    }
                    else
                    {
                        Console.WriteLine("Max size(5MB) exceeded! Try again, please.");
                        path = Console.ReadLine(); // Ask again
                    }
                }
                else
                {
                    Console.WriteLine("Path or file does not exist. Please try again.");
                    path = Console.ReadLine(); // Ask again
                }
            } while (!fileExists || !allowedSize); // While path doesn't exists or size exceeds 5MB

            Console.WriteLine("File allowed!");

            SIZE_FILE   = sizeFile.Length;
            BUFFER_FILE = File.ReadAllBytes(path);
        }

        static void ResetConnection()
        {
            SEQ              = 109876;
            SYN              = 1;
            ACK              = 0;
            MSS              = 4096;
            SEGM             = 1;
            temp             = 0;
            SIZE_FILE        = 0;
            BUFFER_FILE      = null;
            retras           = false;
            tryRetras        = 5;
            tryRetransmiting = 5;
            isRetransmiting  = false;
        }

        static void CountSegm()
        {
            NUM_SEGM = BUFFER_FILE.Length / 576;

            if ((NUM_SEGM * 576) < BUFFER_FILE.Length)
            {
                NUM_SEGM++; // Add 1 if necesary
            }

            // To finish
            NUM_SEGM += 1;
            // Add receiver segments
            NUM_SEGM *= 2;
        }

        public static string RandomString()
        {
            // Get 3 random chars
            int length         = 3;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static bool RetryPacket(string[] info)
        {
            // int.Parse(info[1]) => SEQ | int.Parse(info[2]) => ACK
            if (ACK != 0)
            {
                // Values not expected, retransmiting
                if (ACK != int.Parse(info[1]) || SEQ >= int.Parse(info[2]))
                {
                    isRetransmiting = true;
                    tryRetransmiting--;
                    return true;
                }
                else {
                    isRetransmiting = false;
                }
            }

            isRetransmiting = false;
            return false;
        }

    }
}


