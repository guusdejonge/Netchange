using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MultiClientServer
{
    class Program
    {
        static public int MijnPoort;
        static public Dictionary<int, Connection> Buren = new Dictionary<int, Connection>();                //lijst buren
        static public Dictionary<int, int> Duv = new Dictionary<int, int>();                                //schatting in u van distance u naar v
        static public Dictionary<int, int> Nbuv = new Dictionary<int, int>();                               //node u's preferred neighbor voor v
        static public Dictionary<Tuple<int, int>, int> Ndisuwv = new Dictionary<Tuple<int, int>, int>();    //node u's kennis over w's afstand tot v
        static public int N = 20;

        static void Main(string[] args)
        {
            Console.Title = args[0];
            int numberOfNeighbors = args.Length;
            MijnPoort = int.Parse(args[0]);
            new Server(MijnPoort);

            try
            {
                for (int i = 1; i < numberOfNeighbors; i++)
                {
                    int poort = int.Parse(args[i]);

                    if (poort > MijnPoort)
                    {
                        addBuren(poort, new Connection(poort));
                    }
                    else { }
                }
            }

            catch { Thread.Sleep(100); }
            init();
            ReadInput();
        }

        static public void SendRoutingTable(Connection buur)
        {
            lock(Duv)
            {
                foreach (int v in Duv.Keys)
                {
                    string bericht = "mydist " + MijnPoort + " " + v + " " + Duv[v];
                    buur.SendMessage(bericht);
                }
            }
            
        }

        static void init()
        {
            addOrSetDuv(MijnPoort, 0);
            addOrSetNbuv(MijnPoort, MijnPoort);

            lock(Buren)
            {
                foreach (int buur in Buren.Keys)
                {
                    addOrSetDuv(buur, 1);
                    addOrSetNbuv(buur, buur);

                    updateburen(buur); //stuur naar alle buren je distance naar deze buur
                }
            }
        }

        static void updateburen(int v)    //stuur je nieuwe distance naar v naar alle buren NOTE: alleen gelockt anroepen
        {
            string bericht = "mydist " + MijnPoort + " " + v + " " + readDuv(v);    //dus: "mydist mijnpoort anderepoort afstand"

            foreach (Connection buur in Buren.Values)
            {
                buur.SendMessage(bericht);
            }
        }
        
        static public void Recompute(int v) //alleen als buren gelockt is
        {
            int afstandvoor = readDuv(v);
            bool containsbuur = Nbuv.ContainsKey(v);
            int prefbuurvoor = 0;
            Console.WriteLine("RECOMPUTE NAAR: " + v);
            Console.WriteLine("afstand voor: " + afstandvoor);
            if (containsbuur)
            {
                prefbuurvoor = readNbuv(v);
              //  Console.WriteLine("Contains buur");
            }

            addOrSetDuv(v, N);

            if (v == MijnPoort)                     //als je v zelf bent
            {
                addOrSetDuv(v, 0);
                addOrSetNbuv(v, MijnPoort);
               // Console.WriteLine("Is mijn poort");
            }
            else if (Buren.ContainsKey(v))           // als v in je burenlijst zit
            {
                addOrSetDuv(v, 1);
                addOrSetNbuv(v, v);
               // Console.WriteLine("Contains key");
            }
            else                                    //en anders: kijken wie je preferred neighbour is
            {
              
                lock (Ndisuwv)
                {
                    foreach (Tuple<int, int> tuple in Ndisuwv.Keys)
                    {
                        Console.WriteLine("test van " + tuple.Item1 + "naar: " + tuple.Item2);
                        if (tuple.Item2 == v)    //deze buur (Item1) heeft een afstand naar v
                        {
                            Console.WriteLine("jaaaaaa");
                            if (Ndisuwv[tuple] < readDuv(v) && Ndisuwv[tuple] < 20)
                            {
                                Console.WriteLine("updaten");
                                addOrSetDuv(v, Ndisuwv[tuple] + 1);
                                addOrSetNbuv(v, tuple.Item1);
                            }
                        }
                    }
                }
                Console.WriteLine("JA?");
            }

            if (afstandvoor != readDuv(v))
            {
                Console.WriteLine("Afstand naar " + v + " is nu " + readDuv(v) + " via " + readNbuv(v));
                Console.WriteLine("VERSTUREN NAAR BUREN");
                updateburen(v);
            }
            else if (containsbuur == false || prefbuurvoor != readNbuv(v))
            {
                Console.WriteLine("Afstand naar " + v + " is nu " + readDuv(v) + " via " + readNbuv(v));
            }

            Console.WriteLine("JA2?");
        }

        static public void ReadInput()
        {
            try
            {
                while (true)
                {
                    string[] input = Console.ReadLine().Split(' ');
                    string inputSwitch = input[0];

                    switch (inputSwitch)
                    {
                        case "R":
                            inputR();
                            break;
                        case "B":
                            inputB(input);
                            break;
                        case "C":
                            inputC(input);
                            break;
                        case "D":

                            inputD(input, true);
                            break;
                    }
                }
            }
            catch { } // Verbinding is kennelijk verbroken
        }

        static void inputR()    //laat routingtable zien
        {
            lock(Nbuv)
            {
                foreach (int port in Nbuv.Keys)
                {
                    int dist = readDuv(port);
                    int neigh = Nbuv[port];
                    if (neigh == MijnPoort)
                    {
                        Console.WriteLine(String.Format("{0} {1} local", port, dist));
                    }
                    else
                    {
                        Console.WriteLine(String.Format("{0} {1} {2}", port, dist, neigh));
                    }
                }
            }
            
        }

        static public void inputB(string[] input)  //stuur bericht (input = B poortnummer bericht)
        {
            int port = int.Parse(input[1]);
            Connection verbinding;
            int nbuv;
            lock(Buren)
            {
                if (Buren.TryGetValue(port, out verbinding))
                {
                    verbinding.SendMessage("B " + input[1] + " " + input[2]);
                }
                else if (Nbuv.TryGetValue(port, out nbuv))
                {
                    verbinding = Buren[nbuv];
                    verbinding.SendMessage("B " + input[1] + " " + input[2]);
                }
                else
                {
                    Console.WriteLine("Poort " + port + " is niet bekend");
                }
            }
            
        }

        static void inputC(string[] input)  //maak connectie (input = C poortnummer)
        {
            int poort = int.Parse(input[1]);
            lock (Buren)
            {
                if (!Buren.ContainsKey(poort))
                    {
                        addBuren(poort, new Connection(poort));
                        Recompute(poort);               //recompute!
                    }
            }
        }

        static public void inputD(string[] input, bool sendmessage)  //delete buur (input = D poortnummer)
        {
            int poort = int.Parse(input[1]);
            Connection verbinding;
            lock(Buren)
            {
                if (Buren.TryGetValue(poort, out verbinding))
                {
                    if (sendmessage)
                    {
                        verbinding.SendMessage("D " + Convert.ToString(MijnPoort));
                    }

                    Buren.Remove(poort);
                    removeNdisuwv(poort);
                    addOrSetDuv(poort, N);
                    
                    List<int> veranderd = new List<int>();
                    
                    lock (Nbuv)
                    {
                        foreach (KeyValuePair<int, int> entry in Nbuv)
                        {
                            if (entry.Value == poort)    //deze buur is net gedelete
                            {
                                veranderd.Add(entry.Key); //hier ging hij heen    
                                Console.WriteLine("een");
                                addOrSetDuv(entry.Key, N);
                                Console.WriteLine("twee");
                                //Recompute(entry.Key);
                                Console.WriteLine("drie");
                            }
                        }

                        foreach (int pref in veranderd)
                        {
                            Recompute(pref);
                            Console.WriteLine("vier");
                        }
                    }


                    Recompute(poort);           //recompute!
                   

                   // foreach (int v in veranderd)
                    //{
                    //    Recompute(v);
                    //}
                }
                else
                {
                    Console.WriteLine("Poort " + poort + " is niet bekend");
                }
            }

            
        }

        static public void addBuren(int poort, Connection verbinding)
        {
            lock (Buren)
            {
                Buren.Add(poort, verbinding);
            }
        }

        static public void removeBuren(int poort)
        {
            lock (Buren)
            {
                Buren.Remove(poort);
                foreach (int buur in Buren.Keys)
                {
                    Console.WriteLine(buur);
                }
                
            }
        }

        static public Connection readBuren(int poort)
        {
            lock (Buren)
            {
                return Buren[poort];
            }
        }

        static public void addOrSetDuv(int poort, int afstand)
        {
            lock (Duv)
            {
                if (Duv.ContainsKey(poort))
                {
                    Duv[poort] = afstand;
                }
                else
                {
                    Duv.Add(poort, afstand);
                }    
            }
        }

        static public int readDuv(int poort)
        {
            lock (Duv)
            {
                return Duv[poort];
            }
        }

        static void addOrSetNbuv(int poort, int prefBuurPoort)
        {
            lock (Nbuv)
            {
                if(Nbuv.ContainsKey(poort))
                {
                    Nbuv[poort] = prefBuurPoort;
                }
                else
                {
                    Nbuv.Add(poort, prefBuurPoort);
                }
            }
        }

        static public int readNbuv(int poort)
        {
            lock (Nbuv)
            {
                return Nbuv[poort];
            }
        }

        static public void addOrSetNdisuwv(Tuple<int, int> tuple, int afstand)
        {
            lock (Ndisuwv)
            {
                if (Ndisuwv.ContainsKey(tuple))
                {
                    Ndisuwv[tuple] = afstand;
                }
                else
                {
                    Ndisuwv.Add(tuple, afstand);
                }
            }
        }

        static public int readNdisuwv(Tuple<int, int> tuple)
        {
            lock (Ndisuwv)
            {
                return Ndisuwv[tuple];
            }
        }

        static public void removeNdisuwv(int poort)
        {
            List<Tuple<int,int>> verwijder = new List<Tuple<int, int>>();
            lock (Ndisuwv)
            {
                foreach (Tuple<int, int> tuple in Ndisuwv.Keys)
                {
                    if (tuple.Item1 == poort)    //deze buur is net gedelete
                    {
                        verwijder.Add(tuple);
                    }
                }

                foreach (Tuple<int, int> tuple in verwijder)
                {
                    Ndisuwv.Remove(tuple);
                }
            }
        }
    }
}
