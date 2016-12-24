using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiClientServer
{
    public class ProgramInputHandler : InputHandler
    {
        public override void B(string[] input)
        {
            int port = int.Parse(input[1]);
            Connection verbinding;
            int? nbuv;
            lock (Program.Buren)
            {
                //  if (Buren.TryGetValue(port, out verbinding))
                // {
                //    verbinding.SendMessage("B " + input[1] + " " + input[2]);
                //}
                lock (Program.Nbuv)
                {
                    if (Program.readDuv(port) == Program.N)
                    {
                        Console.WriteLine("Onbereikbaar: " + port);
                    }
                    else if (Program.Nbuv.TryGetValue(port, out nbuv))
                    {
                        verbinding = Program.Buren[(int)nbuv];
                        verbinding.SendMessage("B " + input[1] + " " + input[2]);
                    }
                    else
                    {
                        Console.WriteLine("Poort " + port + " is niet bekend");
                    }
                }
            }
        }

        public override void C(string[] input)
        {
            int poort = int.Parse(input[1]);
            lock (Program.Buren)
            {
                if (!Program.Buren.ContainsKey(poort))
                {
                    Connection verbinding = new Connection(poort);
                    Program.Buren.Add(poort, verbinding);
                    Program.addOrSetDuv(poort, 1);
                    Program.addOrSetNbuv(poort, poort);
                    Program.updateburen(poort);
                    // lock (Program.Duv)
                    // {
                    //     foreach (int bestemming in Program.Duv.Keys)
                    //     {
                    //         Tuple<int, int> tuple = new Tuple<int, int>(poort, bestemming);
                    //         Program.addOrSetNdisuwv(tuple, Program.N + 1);
                    //     }
                    // }
                    Program.SendRoutingTable(poort, verbinding);

                    //Program.addOrSetNbuv(poort, poort);
                    //Program.addOrSetDuv(poort, 21);
                    //Program.Recompute(poort);
                    // Console.WriteLine("hoi1");
                    // Recompute(poort);               //recompute!

                }
            }

            lock (Program.Buren)
            {
                lock (Program.recomputeLocker)
                {
                    Program.Recompute(poort);
                }
            }
        }

        public override void D(string[] input, bool sendMessage)
        {
            int poort = int.Parse(input[1]);

            Connection verbinding;
            lock (Program.Buren)
            {

                if (Program.Buren.TryGetValue(poort, out verbinding))
                {

                    if (sendMessage)
                    {

                        verbinding.SendMessage("D " + Convert.ToString(Program.MijnPoort));
                    }
                    Program.removeNdisuwv(poort);
                    Program.Buren.Remove(poort);

                    Console.WriteLine("Verbroken: " + poort);

                    List<int> veranderd = new List<int>();

                    lock (Program.Nbuv)
                    {
                        foreach (KeyValuePair<int, int?> tuple in Program.Nbuv)
                        {
                            if (tuple.Value == poort) { veranderd.Add(tuple.Key); }
                        }
                    }

                    lock (Program.recomputeLocker)
                    {
                        foreach (int bestemming in veranderd)
                        {

                            Program.Recompute(bestemming);

                        }
                    }

                    Program.Recompute(poort);

                    //    Program.removeNdisuwv(poort);

                    //    List<int> veranderd = new List<int>();
                    //    Program.Buren.Remove(poort);

                    //    lock (Program.Nbuv)
                    //    {
                    //        foreach (KeyValuePair<int, int?> entry in Program.Nbuv)
                    //        {
                    //            if (entry.Value == poort)    //deze buur is net gedelete
                    //            {
                    //                veranderd.Add(entry.Key); //hier ging hij heen
                    //            }
                    //        }

                    //        foreach (int pref in veranderd)
                    //        {
                    //            Program.Recompute(pref);
                    //        }
                    //    }


                    //    Program.Recompute(poort);           //recompute!
                }
                else
                {
                    Console.WriteLine("Poort " + poort + " is niet bekend");
                }
            }
        }

        public override void R()
        {
            lock (Program.Nbuv)
            {
                foreach (int port in Program.Nbuv.Keys)
                {
                    int dist = Program.readDuv(port);
                    int? neigh = Program.Nbuv[port];
                    if (neigh == Program.MijnPoort)
                    {
                        Console.WriteLine(String.Format("{0} {1} local", port, dist));
                    }
                    else
                    {
                        if (dist != Program.N)
                        {
                            Console.WriteLine(String.Format("{0} {1} {2}", port, dist, neigh));
                        }
                    }
                }
            }
        }
    }
}
