using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace Receiver
{

    class Receiver
    {

        public static int SEQ = 20191;
        public static int SYN = 1;
        public static int ACK = 0;
        public static int MSS = 8192;
        public static int SEGM = 1;
        public static int temp = 0;

        static void Main(string[] args)
        {
            string optn         = getMenu();
            int ctn             = 2;
            Socket s            = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");
            UdpClient listener  = new UdpClient(11000);
            IPEndPoint groupEP  = new IPEndPoint(IPAddress.Any, 11000);
            
            /** 
              * CTN = 0: Close connection  
              * CTN = 1: Reset and reconection with new file
              * CTN = 2: First connection  
            **/

            switch (optn)
            {
                case "1":
                    while (SEGM < 6 || ctn > 0)
                    {
                        if(ctn == 1) {
                            Console.Clear();
                            resetConnection();
                        }

                        getConnection(listener, groupEP);
                        ctn = sendResponse(s, broadcast);
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
            Console.WriteLine("1 - Receber dados liga��o UDP");
            Console.WriteLine("0 - Sair");
            Console.WriteLine("*************************");
            optn = Console.ReadLine();

            return optn;
        }

        static void getConnection(UdpClient listener, IPEndPoint groupEP)
        {
            try
            {
                Console.WriteLine("Aguardar dados...");
                byte[] bytes = listener.Receive(ref groupEP);
                string content = Encoding.ASCII.GetString(bytes);
                string[] info = content.Split('_');

                if (MSS >= int.Parse(info[3]))
                {
                    MSS = int.Parse(info[3]);
                }
                
                SYN  = int.Parse(info[0]);
                ACK  = int.Parse(info[1]) + 1;
                SEGM = int.Parse(info[4]) + 1;

                if (SYN == 0 && temp >= 1)
                {
                    SEQ = int.Parse(info[2]);
                    ACK = int.Parse(info[1]) + MSS;
                }
                
                // Show status
                Console.WriteLine("***********");
                Console.WriteLine("SEGMENTO: {0}", SEGM);
                Console.WriteLine("MSS: {0}", MSS);
                Console.WriteLine("SYN: {0}", SYN);
                Console.WriteLine("SEQ: {0}", SEQ);
                Console.WriteLine("ACK: {0}", ACK);
                Console.WriteLine("***********");    
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            // finally
            // {
            //     listener.Close();
            // }
        }

        static int sendResponse(Socket s, IPAddress broadcast)
        {
            string content = SYN.ToString() + '_' + SEQ.ToString() + '_' + ACK.ToString() + '_' + MSS.ToString() + '_' + SEGM.ToString();

            try
            {
                byte[] sendbuf = Encoding.ASCII.GetBytes(content);
                IPEndPoint ep = new IPEndPoint(broadcast, 11001);

                s.SendTo(sendbuf, ep);
                //s.Send(sendbuf);
                Console.WriteLine("Mensagem enviada!");

                temp++;
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }

            if(SEGM >= 6) {
                Console.WriteLine("Deseja continuar à escuta de outros ficheiros?");
                Console.WriteLine("1 - Sim");
                Console.WriteLine("0 - Não");
                string resp = Console.ReadLine();

                return (int.Parse(resp) == 1 ? 1 : 0);
            }

            return 2;
        }

        static void resetConnection(){
            SEQ = 20191;
            SYN = 1;
            ACK = 0;
            MSS = 8192;
            SEGM = 1;
            temp = 0;
        }
    }
}
