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

        static public Dictionary<int, Connection> Buren = new Dictionary<int, Connection>();

        static void Main(string[] args)
        {

            Console.Title = args[0];
            numberOfNeighbors = args.Length - 1;
            
            MijnPoort = int.Parse(args[0]);
            new Server(MijnPoort);

            for(int i = 1; i < numberOfNeighbors; i++)
            {
                int port = int.Parse(args[i]);
                Buren.Add(port, new Connection(port));
            }

            

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
    }
}
