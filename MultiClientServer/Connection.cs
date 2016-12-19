﻿using System;
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

            Program.SendRoutingTable(this);
        }

        // Deze constructor wordt gebruikt als wij SERVER zijn en een CLIENT maakt met ons verbinding
        public Connection(StreamReader read, StreamWriter write)
        {
            Read = read; Write = write;

            // Start het reader-loopje
            new Thread(ReaderThread).Start();

            Program.SendRoutingTable(this);
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
                        case "D":               //de buur is gedelete (aan de andere kant is D .... ingevoerd)
                            inputD(input);
                            break;
                        case "mydist":          //een andere buurt stuurt jou zijn nieuw afstand naar een v
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
            int poort = int.Parse(input[1]);

            Program.removeBuren(poort);

            Program.Recompute(poort);
        }

        void inputMyDist(string[] input)        //input: "mydist u v d"
        {
            int u = int.Parse(input[1]);    //de buur die dit stuurt
            int v = int.Parse(input[2]);    //de node waarnaar zijn afstand is veranderd
            int d = int.Parse(input[3]);    //zijn nieuwe afstand daarnaartoe

            Tuple<int, int> uv = new Tuple<int, int>(u, v);
            
            if(!Program.Duv.Keys.Contains(v))
            {
                Program.addOrSetDuv(v, 20);
            }

            Program.addOrSetNdisuvw(uv, d);         //toevoegen of wijzigen nieuwe d
            
            Program.Recompute(v);       //en recompute
        }
    }
}
