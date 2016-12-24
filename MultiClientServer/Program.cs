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
        static public Dictionary<int, int?> Nbuv = new Dictionary<int, int?>();                               //node u's preferred neighbor voor v
        static public Dictionary<Tuple<int, int>, int> Ndisuwv = new Dictionary<Tuple<int, int>, int>();    //node u's kennis over w's afstand tot v

        static public int N = 20;
        static public int aantalBuren = 0;
        static public int verwerkteBuren = 0;

        static public object NLocker = new object();
        static public object verwerktLocker = new object();
        static public object recomputeLocker = new object();

        static public bool initKlaar = false;

        static InputHandler inputHandler;

        static void Main(string[] args)
        {
            Console.Title = args[0];
            aantalBuren = args.Length - 1;
            MijnPoort = int.Parse(args[0]);
            new Server(MijnPoort);

            //Buren toevoegen die zijn ingegeven opstarten
            try
            {
                for (int i = 0; i < aantalBuren; i++)
                {
                    int poort = int.Parse(args[i + 1]);
                    if (poort > MijnPoort)
                    {
                        addBuren(poort, new Connection(poort));
                        Console.WriteLine("Verbonden: " + poort);
                      //  lock (verwerktLocker)
                      //  {
                      //      verwerkteBuren++;
                      // }
                    }
                    else {}
                }
            }

            //Als buur nog niet is opgestart en Connection faalt, even slapen...
            catch { Thread.Sleep(50); }


            inputHandler = new ProgramInputHandler();

            init();
            //foreach (Connection verbinding in Buren.Values)
            //{
            //    string bericht = "mydist " + MijnPoort + " " + MijnPoort + " " + readDuv(MijnPoort);    //dus: "mydist mijnpoort anderepoort afstand"
            //    verbinding.SendMessage(bericht);

            //}

            updateburen(MijnPoort);

            ReadInput();
        }

        static public void SendRoutingTable(int poort, Connection buur)
        {
            lock (Duv)
            {
                foreach (int v in Duv.Keys)
                {
                    string bericht = "mydist " + MijnPoort + " " + v + " " + Duv[v];
                    buur.SendMessage(bericht);
                    //Console.WriteLine("Verstuurd ROUTINGTABLE" + " " + bericht + " naar " +  poort );

                }
            }
            buur.SendMessage("mydist " + MijnPoort + " " + MijnPoort + " " + 0);

        }

        static void init()
        {
            //Gegevens voor eigen proces instellen
            addOrSetDuv(MijnPoort, 0);
            addOrSetNbuv(MijnPoort, MijnPoort);

           //while (verwerkteBuren != aantalBuren)
           // {
           //     Thread.Sleep(100);
           // }

            lock (Buren)
            {
                //Aan buren doorgeven dat mijn afstand naar mezelf 0 is
                updateburen(MijnPoort);

                //Elke buur aan Duv en Nbuv toevoegen met maximale afstand of preferred neighbour
                foreach (int buur in Buren.Keys)
                {
                    addOrSetDuv(buur, N);
                    addOrSetNbuv(buur, null);
                }

                //Alle combinaties van buren in ndisuwv zetten met maximale afstand 
                foreach (int buur1 in Buren.Keys)
                {


                    foreach (int buur2 in Buren.Keys)
                    {
                        if (buur1 < buur2)
                        {
                            Tuple<int, int> tuple = new Tuple<int, int>(buur1, buur2);
                            addOrSetNdisuwv(tuple, N);
         
                        }
                    }



                }
            }

            initKlaar = true;
            //Console.WriteLine("Init klaar");
        }

        //Stuur mijn nieuwe distance voor v naar alle buren 
        //NOTE: alleen met Buren gelockt anroepen
        static public void updateburen(int v)   
        {
            string bericht = "mydist " + MijnPoort + " " + v + " " + readDuv(v);    

            foreach (KeyValuePair<int, Connection> buur in Buren)
            {
                buur.Value.SendMessage(bericht);
                //Console.WriteLine("Verstuurd " + " " + bericht + " naar" + " " + buur.Key);

            }
        }

        static public void Recompute(int v) //alleen als buren gelockt is
        {
            //Als ik v ben dan afstand op 0 en mezelf als preferred neighbor
            if (v == MijnPoort)
            {
                addOrSetDuv(MijnPoort, 0);
                addOrSetNbuv(MijnPoort, MijnPoort);
            }

            
            
            else
            {
                //Afstand en preferred neigbour initialiseren als N en null
                int afstand = N;
                int? prefBuurVoor = null;

                //Als v al bekend is wordt afstandVoor de oude afstand
                int afstandVoor = 21;
                if (Duv.ContainsKey(v))
                {
                    afstandVoor = readDuv(v);
                }
                
                lock (Ndisuwv)
                {
                    foreach (KeyValuePair<Tuple<int, int>, int> tuple in Ndisuwv)
                    {
                        //Als een buur een afstand voor v, of Item2, weet
                        if (tuple.Key.Item2 == v)
                        {
                            //Als de afstand kleiner is dan de vorige kleinste afstand
                            if (tuple.Value < afstand)
                            {
                                //Mijn afstand is deze afstand + 1
                                afstand = 1 + tuple.Value;
                                prefBuurVoor = tuple.Key.Item1;
                            }
                        }
                    }
                }
                if (afstand < N)
                {
                    addOrSetDuv(v, afstand);
                    addOrSetNbuv(v, prefBuurVoor);
                }
                else
                {
                    addOrSetDuv(v, N);
                    addOrSetNbuv(v, null);
                    Console.WriteLine("Onbereikbaar: " + v);
                }

                if (afstand != afstandVoor)
                {
                    updateburen(v);
                }
            }

            //int afstandvoor = readDuv(v);
            //bool containsbuur = Nbuv.ContainsKey(v);
            //int prefbuurvoor = 0;
            //if (containsbuur)
            //{
            //    prefbuurvoor = readNbuv(v);
            //}

            //addOrSetDuv(v, N);

            //if (v == MijnPoort)                     //als je v zelf bent
            //{
            //    addOrSetDuv(v, 0);
            //    addOrSetNbuv(v, MijnPoort);
            //}
            //else if (Buren.ContainsKey(v))           // als v in je burenlijst zit
            //{
            //    addOrSetDuv(v, 1);
            //    addOrSetNbuv(v, v);
            //}
            //else                                    //en anders: kijken wie je preferred neighbour is
            //{

            //    lock (Ndisuwv)
            //    {
            //        foreach (Tuple<int, int> tuple in Ndisuwv.Keys)
            //        {
            //            if (tuple.Item2 == v)    //deze buur (Item1) heeft een afstand naar v
            //            {
            //                if (Ndisuwv[tuple] < readDuv(v) && Ndisuwv[tuple] <= N)
            //                {
            //                    addOrSetDuv(v, Ndisuwv[tuple] + 1);
            //                    addOrSetNbuv(v, tuple.Item1);
            //                }
            //            }
            //        }
            //    }
            //}

            //if (afstandvoor != readDuv(v))
            //{
            //    Console.WriteLine("Afstand naar " + v + " is nu " + readDuv(v) + " via " + readNbuv(v));
            //    updateburen(v);
            //}
            //else if (containsbuur == false || prefbuurvoor != readNbuv(v))
            //{
            //    updateburen(v);
            //    Console.WriteLine("Afstand naar " + v + " is nu " + readDuv(v) + " via " + readNbuv(v));
            //}


            Console.WriteLine("Afstand naar " + v + " is nu " + readDuv(v) + " via " + readNbuv(v));
        }

        static public void ReadInput()
        {
            try
            {
                while (true)
                {
                    string[] input = Console.ReadLine().Split(' ');
                    string inputSwitch = input[0];

                    lock (inputHandler)
                    {
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
            }
            catch { } // Verbinding is kennelijk verbroken
        }

        static public void addBuren(int poort, Connection verbinding)
        {
            lock (Buren)
            {
                try
                {
                    Buren.Add(poort, verbinding);
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex);
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
                    //Console.WriteLine("Aangepast: " + poort + " " + afstand);
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

        static public void addOrSetNbuv(int poort, int? prefBuurPoort)
        {
            lock (Nbuv)
            {
                if (Nbuv.ContainsKey(poort))
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
                return (int)Nbuv[poort];
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
            List<Tuple<int, int>> verwijder = new List<Tuple<int, int>>();
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
