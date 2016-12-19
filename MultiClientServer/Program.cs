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

        static object BurenLocker = new object();
        static object DuvLocker = new object();
        static object NbuvLocker = new object();
        static object NdisuwvLocker = new object();

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
            lock(DuvLocker)
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

            foreach (int buur in Buren.Keys)
            {
                addOrSetDuv(buur, 1);
                addOrSetNbuv(buur, buur);

                updateburen(buur); //stuur naar alle buren je distance naar deze buur
            }
        }

        static void updateburen(int v)    //stuur je nieuwe distance naar v naar alle buren
        {
            string bericht = "mydist " + MijnPoort + " " + v + " " + Duv[v];    //dus: "mydist mijnpoort anderepoort afstand"

            lock(BurenLocker)
            {
                foreach (Connection buur in Buren.Values)
                {
                    buur.SendMessage(bericht);
                }
            }
           
            
        }
        
        static public void Recompute(int v)
        {
            int afstandvoor = Duv[v];

            if (v == MijnPoort)                     //als je v zelf bent
            {
                addOrSetDuv(v, 0);
                addOrSetNbuv(v, MijnPoort);
            }
            else if (Buren.ContainsKey(v))           // als v in je burenlijst zit
            {
                addOrSetDuv(v, 1);
                addOrSetNbuv(v, v);
            }
            else                                    //en anders: kijken wie je preferred neighbour is
            {
                foreach (Tuple<int, int> tuple in Ndisuwv.Keys)
                {
                    if (tuple.Item2 == v)    //deze buur (Item1) heeft een afstand naar v
                    {
                        if (Ndisuwv[tuple] < Duv[v] && Ndisuwv[tuple] < 20)
                        {
                            Duv[v] = Ndisuwv[tuple] + 1;
                            Nbuv[v] = tuple.Item1;
                        }
                    }
                }
            }

            if (afstandvoor != Duv[v])
            {
                updateburen(v);
            }
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
                            inputD(input);
                            break;
                    }
                }
            }
            catch { } // Verbinding is kennelijk verbroken
        }

        static void inputR()    //laat routingtable zien
        {
            foreach (int port in Nbuv.Keys)
            {
                int dist = Duv[port];
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

        static void inputB(string[] input)  //stuur bericht (input = B poortnummer bericht)
        {
            int port = int.Parse(input[1]);
            Connection verbinding;
            if (Buren.TryGetValue(port, out verbinding))
            {
                verbinding.SendMessage(input[2]);
            }
            else
            {
                Console.WriteLine("Poort " + port + " is niet bekend");
            }
        }

        static void inputC(string[] input)  //maak connectie (input = C poortnummer)
        {
            int poort = int.Parse(input[1]);
            addBuren(poort, new Connection(poort));
            Recompute(poort);               //recompute!
        }

        static void inputD(string[] input)  //delete buur (input = D poortnummer)
        {
            int poort = int.Parse(input[1]);
            Connection verbinding;
            if (Buren.TryGetValue(poort, out verbinding))
            {
                verbinding.SendMessage("D " + Convert.ToString(MijnPoort));
                removeBuren(poort);
                Recompute(poort);           //recompute!
            }
            else
            {
                Console.WriteLine("Poort " + poort + " is niet bekend");
            }
        }

        static public void addBuren(int poort, Connection verbinding)
        {
            lock (BurenLocker)
            {
                Buren.Add(poort, verbinding);
            }
        }

        static public void removeBuren(int poort)
        {
            lock (BurenLocker)
            {
                Buren.Remove(poort);
            }
        }

        static public void addOrSetDuv(int poort, int afstand)
        {
            lock (DuvLocker)
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

        static void addOrSetNbuv(int poort, int prefBuurPoort)
        {
            lock (NbuvLocker)
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

        static public void addOrSetNdisuvw(Tuple<int, int> tuple, int afstand)
        {
            lock (NdisuwvLocker)
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
    }
}
