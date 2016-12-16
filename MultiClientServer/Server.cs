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
        object locker = new object();
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

                    Console.WriteLine("Client maakt verbinding: " + zijnPoort);



                    lock (locker)
                    {
                        // Zet de nieuwe verbinding in de verbindingslijst
                        Program.Buren.Add(zijnPoort, new Connection(clientIn, clientOut));

                        if (Program.Duv.ContainsKey(zijnPoort)) { Program.Duv[zijnPoort] = 1; }
                        else { Program.Duv.Add(zijnPoort, 1); }

                        if (Program.Nbuv.ContainsKey(zijnPoort)) { Program.Nbuv[zijnPoort] = zijnPoort; }
                        else { Program.Nbuv.Add(zijnPoort,zijnPoort); }

                    }

                    //Program.Recompute(zijnPoort);

                }
                catch { Thread.Sleep(100); }
            }
        }
    }
}
