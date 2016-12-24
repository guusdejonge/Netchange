using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace MultiClientServer
{
    class Server
    {
        public Server(int port)
        {
            // Luister op de opgegeven poort naar verbindingen
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            // Start een aparte thread op die verbindingen aanneemt
            new Thread(() => AcceptLoop(server)).Start();
        }

        private void AcceptLoop(TcpListener handle)
        {
            while (true)
            {
                try
                {
                    TcpClient client = handle.AcceptTcpClient();
                    StreamReader clientIn = new StreamReader(client.GetStream());
                    StreamWriter clientOut = new StreamWriter(client.GetStream());
                    clientOut.AutoFlush = true;

                    // De server weet niet wat de poort is van de client die verbinding maakt, de client geeft dus als onderdeel van het protocol als eerst een bericht met zijn poort
                    int zijnPoort = int.Parse(clientIn.ReadLine().Split()[1]);
                    //Console.WriteLine("Client maakt verbinding: " + zijnPoort);
                    Connection verbinding = new Connection(clientIn, clientOut);

                    Program.addBuren(zijnPoort, verbinding);
                    lock (Program.verwerktLocker)
                    {
                        Program.verwerkteBuren++;
                    }

                    

                    //lock (Program.Duv)
                    //{
                     //   foreach(int bestemming in Program.Duv.Keys)
                    //    {
                    //        Tuple<int, int> tuple = new Tuple<int, int>(zijnPoort, bestemming);
                    //        Program.addOrSetNdisuwv(tuple, Program.N + 1);
                    //    }
                    //}
                    //Program.addBuren(zijnPoort, verbinding);   // Zet de nieuwe verbinding in de verbindingslijst
                    Program.addOrSetDuv(zijnPoort, 1);
                    Program.addOrSetNbuv(zijnPoort, zijnPoort);
                    Program.updateburen(zijnPoort);
                    Console.WriteLine(zijnPoort + " is nu mijn buur");

                    lock (Program.Buren)
                    {
                        lock (Program.recomputeLocker)
                        {
                            Program.Recompute(zijnPoort);
                        }
                    }

                    Program.SendRoutingTable(zijnPoort, verbinding);
                }
                catch { Thread.Sleep(10); }
            }
        }
    }
}
