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
        static int numberOfNeighbors;
        static object locker = new object();
        static public Dictionary<int, Connection> Buren = new Dictionary<int, Connection>();
        static public Dictionary<int, int> Duv = new Dictionary<int, int>(); //schatting in u van distance u naar v
        static public Dictionary<int, int> Nbuv = new Dictionary<int, int>(); //node u's preferred neighbor voor v
        static public Dictionary<Tuple<int,int>, int> ndisuwv = new Dictionary<Tuple<int, int>,int>(); //node u's kennis over w's afstand tot v
        static public int N = 20;

        static void Main(string[] args)
        {

            Console.Title = args[0];
            numberOfNeighbors = args.Length;

            MijnPoort = int.Parse(args[0]);
            new Server(MijnPoort);

            int teller = 0;


            try
            {
                for (int i = 1; i < numberOfNeighbors; i++)
                {
                    int port = int.Parse(args[i]);
                    lock (locker)
                    {
                        if (port > MijnPoort)
                        {
                            Buren.Add(port, new Connection(port));
                            teller++;
                        }
                        else { }
                    }
                }
            }

            catch { Thread.Sleep(100); }

            ReadInput();
        }

        static void init()
        {
            

            foreach (int buur in Buren.Keys)
            {
                foreach(int buur2 in Buren.Keys)
                {
                    Tuple<int,int> t = new Tuple<int,int> (buur, buur2);
                    ndisuwv.Add(t, N);
                }
            }

            Duv.Add(MijnPoort, 0);
            Nbuv.Add(MijnPoort, MijnPoort); //jezelf is dus local

            foreach(int buur in Buren.Keys)
            {
                Duv.Add(buur, 1);
                Nbuv.Add(buur, 0); //zelfde buur is undefined
            }
        }

        static void RecomputeAll()
        {
            foreach(int v in Duv.Keys)
            {
                Recompute(v);
            }
        }

        static void Recompute(int v)
        {
            int DuvFirst = Duv[v];

            if (v == MijnPoort)
            {
                Duv[v] = 0;
                Nbuv[v] = MijnPoort;
            }
            else
            {
                int laagstebuur = Buren.First().Key;
                foreach(int buur in Buren.Keys)
                {
                    if(ndisuwv[new Tuple<int,int>(buur,v)] < ndisuwv[new Tuple<int,int> (laagstebuur,v)])
                    {
                        laagstebuur = buur;
                    }
                }
                int d = 1 + ndisuwv[new Tuple<int, int>(laagstebuur, v)];

                if(d < N)   //hier wel te bereiken
                {
                    Duv[v] = d;
                    Nbuv[v] = laagstebuur;
                }
                else    //hier niet te bereiken
                {
                    Duv[v] = N;
                    Nbuv[v] = 0;
                }
            }

            if (DuvFirst != Duv[v])
            {
                //foreach(int buur)
            }
        }

        static public void ReadInput()
        {
            try
            {
                while (true)
                {
                    string[] input = Console.ReadLine().Split(' ');
                    if (input[0] == "R")
                    {
                        foreach(int port in Buren.Keys) { Console.WriteLine(port); }
                    }
                    else if(input[0] == "B")
                    {
                        int port = int.Parse(input[1]);
                        Connection verbinding;
                        if (Buren.TryGetValue(port, out verbinding))
                        {
                            verbinding.SendMessage(input[2]);
                            Buren.Remove(port);
                        }
                        else
                        {
                            Console.WriteLine("Poort " + port + " is niet bekend");
                        }

                        
                    }
                    else if (input[0] == "C")
                    {
                        int poort = int.Parse(input[1]);
                        Buren.Add(poort, new Connection(poort));
                    }
                    else if (input[0] == "D")
                    {
                        
                        int port = int.Parse(input[1]);
                        Connection verbinding;
                        if (Buren.TryGetValue(port, out verbinding))
                        {
                            verbinding.SendMessage(String.Format("D {0}", Convert.ToString(MijnPoort)));
                            Buren.Remove(port);
                        }
                        else
                        {
                            Console.WriteLine("Poort " + port + " is niet bekend");
                        }
                        
                        
                    }
                }
                    
            }
            catch { } // Verbinding is kennelijk verbroken
        }
    }
}
