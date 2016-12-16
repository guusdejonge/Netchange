using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MultiClientServer
{
    class Connection
    {
        public StreamReader Read;
        public StreamWriter Write;

        // Connection heeft 2 constructoren: deze constructor wordt gebruikt als wij CLIENT worden bij een andere SERVER
        public Connection(int port)
        {
            TcpClient client = new TcpClient("localhost", port);
            Read = new StreamReader(client.GetStream());
            Write = new StreamWriter(client.GetStream());
            Write.AutoFlush = true;

            // De server kan niet zien van welke poort wij client zijn, dit moeten we apart laten weten
            Write.WriteLine("Poort: " + Program.MijnPoort);

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
        }

        // Deze constructor wordt gebruikt als wij SERVER zijn en een CLIENT maakt met ons verbinding
        public Connection(StreamReader read, StreamWriter write)
        {
            Read = read; Write = write;

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
        }

        // LET OP: Nadat er verbinding is gelegd, kun je vergeten wie er client/server is (en dat kun je aan het Connection-object dus ook niet zien!)

        // Deze loop leest wat er binnenkomt en print dit
        public void ReaderThread()
        {
            try
            {
                while (true)
                {
                    string[] input = Read.ReadLine().Split(' ');
                    string inputSwitch = input[0];

                    switch (inputSwitch)
                    {
                        case "D":
                            inputD(input);
                            break;
                        case "mydist":
                            inputMyDist(input);
                            break;
                    }  
                }
            }
            catch { } // Verbinding is kennelijk verbroken
        }

        public void SendMessage(string message)
        {
            Write.WriteLine(message);
        }

        void inputD(string[] input)
        {
            int port = int.Parse(input[1]);

            Program.Buren.Remove(port);
        }

        void inputMyDist(string[] input)
        {
            int v = int.Parse(input[1]);
            int Dwv = int.Parse(input[2]);
            int w = int.Parse(input[3]);

            if (!Program.Duv.ContainsKey(v))        //stel hij heeft v nog niet
            {
                Program.Duv.Add(v, 20);
                foreach (int node in Program.Duv.Keys)
                {
                    if (node < v)
                    {
                        Program.ndisuwv[new Tuple<int, int>(node, v)] = 20;
                    }
                    else
                    {
                        Program.ndisuwv[new Tuple<int, int>(v, node)] = 20;
                    }
                }

            }

            Program.Duv[v] = Dwv + 1;
            if (v > w)
            {
                Program.ndisuwv[new Tuple<int, int>(w, v)] = Dwv;
            }
            else
            {
                Program.ndisuwv[new Tuple<int, int>(v, w)] = Dwv;
            }


            Program.Recompute(v);

        }
    }
}
