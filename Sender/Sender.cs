using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UDP_Multicast_Sender
{
    class Sender
    {
        //public static IDictionary<string, int> configs = new Dictionary<string, int>();
        //public static int[] configs = new int[2];
        public static int SEQ = 109876;
        public static int SYN = 1;
        public static int ACK = 0;
        public static int MSS = 4096;
        public static int SEGM = 1;
        public static int temp = 0;
        public static int SIZE_FILE = 0;
        public static byte[] BUFFER_FILE;
        public static float NUM_SEGM = 4; // Default
        public static UdpClient listener = new UdpClient(11001);
        public static bool retras = false;
        public static int tryRetras = 5; // Number retransmissions

        static void Main(string[] args)
        {
            string optn = GetMenu();
            int ctn = 2;
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 11000);

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
                        // Reset connection
                        if (ctn == 1)
                        {
                            Console.Clear();
                            ResetConnection();
                        }
                        
                        if(ctn == 0 && tryRetras < 0) {
                            ResetConnection();
                        }
                        else if(tryRetras == 0) {
                            break; // Timeout
                        }

                        // Open connection
                        string path = GetConnection(s, broadcast);
                        // Receive data
                        ctn         = ReceiveResponse(groupEP);

                        // Send file
                        if(ctn == 2)
                        {
                            SendFile(s, broadcast, path, groupEP);
                        }
                    }
                    break;
                case "0":
                    Console.WriteLine("Obrigado!");
                    break;
                default:
                    Console.WriteLine("Obrigado!");
                    break;
            }

            Console.WriteLine("Pressione ENTER para fechar!");
            Console.ReadLine();
        }

        static string GetMenu()
        {
            string optn;

            Console.WriteLine("*************************");
            Console.WriteLine("MENU");
            Console.WriteLine("1 - Iniciar ligação UDP");
            Console.WriteLine("0 - Sair");
            Console.WriteLine("*************************");
            optn = Console.ReadLine();

            return optn;
        }

        static string GetConnection(Socket s, IPAddress broadcast)
        {
            string path = null;

            // Only first time
            if (SYN == 1)
            {
                Console.WriteLine("Indique o caminho do ficheiro" + @" Exemplo:(C:\Users\utilizador\Desktop\nomeFicheiro.extensao)");
                path = @"/Users/joelcarvalho/Desktop/horario.png";
                //path = Console.ReadLine();
                FileRequest(path);
            }

            // Count number of required segments
            CountSegm();

            string content = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() + '_' + MSS.ToString() + '_' + SEGM.ToString() + '_' + NUM_SEGM.ToString();
            try
            {
                // Config send 
                byte[] sendbuf = Encoding.ASCII.GetBytes(content);
                IPEndPoint ep = new IPEndPoint(broadcast, 11000);

                // Send content
                s.SendTo(sendbuf, ep);
                Console.WriteLine("Mensagem enviada!");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            return path;
        }

        static void SendFile(Socket s, IPAddress broadcast, string path, IPEndPoint groupEP)
        {
            byte[] buffer = BUFFER_FILE;
            byte[] split  = new byte[576];
            int pos       = 575;
            int cont      = 0; // Number iterations
            int percent   = 0;

            for (int i = 0; i < buffer.Length; i+= pos)
            {
                int comp = pos;
                cont++;
                Array.Copy(buffer, split, comp); // Copy position to array
                string txt = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() + '_' + MSS.ToString() + '_' + SEGM.ToString() + '_' + NUM_SEGM.ToString() + '_' + split;
                try
                {
                    byte[] sendbuf = Encoding.ASCII.GetBytes(txt);
                    IPEndPoint ep = new IPEndPoint(broadcast, 11000);

                    // Send content
                    s.SendTo(sendbuf, ep);
                    
                    // Calculate percentage 
                    percent = (((pos*cont) * 100)/SIZE_FILE < 100 ? ((pos*cont) * 100)/SIZE_FILE : 100);
                    
                    Console.WriteLine("A enviar ficheiro...");
                    Console.WriteLine("Estado: {0}%", percent);

                    // Waiting response
                    if(percent < 100) {
                        ReceiveResponse(groupEP); // Get ACK, SEQ...
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
            }   
        }

        static int ReceiveResponse(IPEndPoint groupEP)
        {
            Console.WriteLine("Aguardar dados...");
            listener.Client.ReceiveTimeout = 5000; // 5s timemout

            try {
                byte[] bytes = listener.Receive(ref groupEP);
                string content = Encoding.ASCII.GetString(bytes);
                string[] info = content.Split('_');
                
                // If ACK == (SEQ+1) and SYN_Receiver == 1 then connection granted
                if (int.Parse(info[2]) == (SEQ + 1) && int.Parse(info[0]) == 1)
                {
                    Console.WriteLine("***********");
                    Console.WriteLine("Ligação garantida!");
                    SYN = 0; MSS = 0;
                }
                else
                {
                    // SE NÃO FOR GARANTIDA, FAZER ALGO.....
                    Console.WriteLine("***********");
                }

                if (MSS >= int.Parse(info[3]))
                {
                    MSS = int.Parse(info[3]);
                }

                ACK = int.Parse(info[1]) + 1;
                SEQ = int.Parse(info[2]);
                SEGM = int.Parse(info[4]) + 1;

                if (SYN == 0 && temp >= 1)
                {
                    SEQ = int.Parse(info[2]) + MSS;
                    ACK = int.Parse(info[1]) + 1;
                }

                // Show status
                Console.WriteLine("SEGMENTO: {0}", SEGM);
                Console.WriteLine("MSS: {0}", MSS);
                Console.WriteLine("SYN: {0}", SYN);
                Console.WriteLine("SEQ: {0}", SEQ);
                Console.WriteLine("ACK: {0}", ACK);
                Console.WriteLine("***********");
                temp++;

                if (SEGM >= NUM_SEGM)
                {
                    Console.WriteLine("Deseja enviar outra ficheiro?");
                    Console.WriteLine("1 - Sim");
                    Console.WriteLine("0 - Não");
                    string resp = Console.ReadLine();

                    return (int.Parse(resp) == 1 ? 1 : 0);
                }

                return 2;

            } catch(Exception e) {
                retras = true;
                tryRetras--;
                Console.WriteLine("Erro, na receção da resposta! Tentativas disponíveis: {0}", tryRetras);
                return 0;
            }
        }

        static void FileRequest(String path)
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
                        Console.WriteLine("Tamanho máximo(5MB) excedido! Tente novamente, por favor.");
                        path = Console.ReadLine(); // Ask again
                    }
                }
                else
                {
                    Console.WriteLine("Caminho ou ficheiro inexistente. Tente novamente, por favor.");
                    path = Console.ReadLine(); // Ask again
                }
            } while (!fileExists || !allowedSize); // While path doesn't exists or size exceeds 5MB

            Console.WriteLine("Ficheiro permitido!");

            SIZE_FILE   = sizeFile.Length;
            BUFFER_FILE = File.ReadAllBytes(path);
        }

        static void ResetConnection()
        {
            SEQ         = 109876;
            SYN         = 1;
            ACK         = 0;
            MSS         = 4096;
            SEGM        = 1;
            temp        = 0;
            SIZE_FILE   = 0;
            BUFFER_FILE = null;
            retras      = false;
            tryRetras   = 5;
        }

        static void CountSegm() {

            NUM_SEGM = BUFFER_FILE.Length/576;

            if((NUM_SEGM*576) < BUFFER_FILE.Length){
                NUM_SEGM++; // Add 1 if necesary
            }

            // To finish
            NUM_SEGM += 1;
            // Add receiver segments
            NUM_SEGM *= 2;
        }
    }
}


