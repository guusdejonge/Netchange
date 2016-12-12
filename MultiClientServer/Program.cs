﻿using System;
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

            //while (true)
            //{
            //    string input = Console.ReadLine();
            //    if (input.StartsWith("verbind"))
            //    {
            //        int poort = int.Parse(input.Split()[1]);
            //        if (Buren.ContainsKey(poort))
            //            Console.WriteLine("Hier is al verbinding naar!");
            //        else
            //        {
            //            // Leg verbinding aan (als client)
            //            Buren.Add(poort, new Connection(poort));
            //        }
            //    }
            //    else
            //    {
            //        // Stuur berichtje
            //        string[] delen = input.Split(new char[] { ' ' }, 2);
            //        int poort = int.Parse(delen[0]);
            //        if (!Buren.ContainsKey(poort))
            //            Console.WriteLine("Hier is al verbinding naar!");
            //        else
            //            Buren[poort].Write.WriteLine(MijnPoort + ": " + delen[1]);
            //    }
            //}
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
                        Console.WriteLine("R doet het");
                    }
                    else if(input[0] == "B")
                    {
                        Connection test = Buren[int.Parse(input[1])];
                        test.SendMessage(input[2]);
                    }
                    else if (input[0] == "C")
                    {
                        int poort = int.Parse(input[1]);
                        Buren.Add(poort, new Connection(poort));
                    }
                }
                    
            }
            catch { } // Verbinding is kennelijk verbroken
        }
    }
}
