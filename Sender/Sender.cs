using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

namespace Sender
{
    public class Sender
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
        public static UdpClient listener = new UdpClient(11001);

        static void Main(string[] args)
        {
            string optn         = getMenu();
            int ctn             = 2;
            Socket s            = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");
            IPEndPoint groupEP  = new IPEndPoint(IPAddress.Any, 11000);

            /** 
              * CTN = 0: Close connection  
              * CTN = 1: Reset and reconection with new file
              * CTN = 2: First connection  
            **/

            switch (optn)
            {
                case "1":
                    while (SEGM < 7 || ctn > 0)
                    {
                        if(ctn == 1) {
                            Console.Clear();
                            resetConnection();  
                        }

                        getConnection(s, broadcast);
                        ctn = receiveResponse(groupEP);
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
        static string getMenu()
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

        static void getConnection(Socket s, IPAddress broadcast)
        {
            string path = null;

            // Only first time
            if(SYN == 1)
            {
                Console.WriteLine("Indique o caminho do ficheiro" + @" Exemplo:(C:\Users\utilizador\Desktop\nomeFicheiro.extensao)");
                path = @"/Users/joelcarvalho/Desktop/teste.txt";
                //path = Console.ReadLine();

                fileRequest(path);
            }

            //configs.Add("SEQ", 109876);
            //configs.Add("SYN", 1);
            //configs[0] = 109876;
            //configs[1] = 1;

            string content = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() + '_' + MSS.ToString() + '_' + SEGM.ToString();
            try
            {   
                byte[] sendbuf = Encoding.ASCII.GetBytes(content);
                IPEndPoint ep  = new IPEndPoint(broadcast, 11000);

                s.SendTo(sendbuf, ep);
                Console.WriteLine("Mensagem enviada!");                
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
        }

        static int receiveResponse(IPEndPoint groupEP)
        {
            Console.WriteLine("Aguardar dados...");
            byte[] bytes   = listener.Receive(ref groupEP);
            string content = Encoding.ASCII.GetString(bytes);
            string[] info  = content.Split('_');

            // If ACK == (SEQ+1) and SYN_Receiver == 1 then connection granted
            if(int.Parse(info[2]) == (SEQ + 1) && int.Parse(info[0]) == 1){
                Console.WriteLine("***********");
                Console.WriteLine("Ligação garantida!");
                SYN = 0; MSS = 0;
            }
            else {
                // SE NÃO FOR GARANTIDA, FAZER ALGO.....
                Console.WriteLine("***********");
            }

            if (MSS >= int.Parse(info[3]))
            {
                MSS = int.Parse(info[3]);
            }

            ACK  = int.Parse(info[1]) + 1;
            SEQ  = int.Parse(info[2]);
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

            if(SEGM >= 7) {
                Console.WriteLine("Deseja enviar outra ficheiro?");
                Console.WriteLine("1 - Sim");
                Console.WriteLine("0 - Não");
                string resp = Console.ReadLine();

                return (int.Parse(resp) == 1 ? 1 : 0);
            }

            return 2;
        }

        static void fileRequest(String path) {
            byte[] sizeFile  = null;
            bool allowedSize = false;
            bool fileExists  = false;

            do
            {
                if (File.Exists(path))
                {
                    fileExists = true;
                    sizeFile = File.ReadAllBytes(path);

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

            Console.WriteLine("Ficheiro permitido");
            Console.WriteLine("TAMANHO {0}", sizeFile.Length);
            SIZE_FILE = sizeFile.Length;

        }

        static void resetConnection(){
            SEQ = 109876;
            SYN = 1;
            ACK = 0;
            MSS = 4096;
            SEGM = 1;
            temp = 0;
            SIZE_FILE = 0;
        }
    }
}

