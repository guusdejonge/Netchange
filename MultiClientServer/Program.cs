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
        static public object NLocker = new object();
        static InputHandler inputHandler;

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

            catch { Thread.Sleep(10); }
            inputHandler = new ProgramInputHandler();

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
            if (containsbuur)
            {
                prefbuurvoor = readNbuv(v);
            }

            addOrSetDuv(v, N);

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
              
                lock (Ndisuwv)
                {
                    foreach (Tuple<int, int> tuple in Ndisuwv.Keys)
                    {
                        if (tuple.Item2 == v)    //deze buur (Item1) heeft een afstand naar v
                        {
                            if (Ndisuwv[tuple] < readDuv(v) && Ndisuwv[tuple] <= N)
                            {
                                addOrSetDuv(v, Ndisuwv[tuple] + 1);
                                addOrSetNbuv(v, tuple.Item1);
                            }
                        }
                    }
                }
            }

            if (afstandvoor != readDuv(v))
            {
                Console.WriteLine("Afstand naar " + v + " is nu " + readDuv(v) + " via " + readNbuv(v));
                updateburen(v);
            }
            else if (containsbuur == false || prefbuurvoor != readNbuv(v))
            {
                updateburen(v);
                Console.WriteLine("Afstand naar " + v + " is nu " + readDuv(v) + " via " + readNbuv(v));
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
                            inputHandler.R();
                            break;
                        case "B":
                            inputHandler.B(input);
                            break;
                        case "C":
                            inputHandler.C(input);
                            break;
                        case "D":
                            inputHandler.D(input, true);
                            break;
                    }
                }
            }
            catch { } // Verbinding is kennelijk verbroken
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
                    lock (NLocker)
                    {
                      // N = 5 + Duv.Count();
                    }
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

        static public void addOrSetNbuv(int poort, int prefBuurPoort)
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
