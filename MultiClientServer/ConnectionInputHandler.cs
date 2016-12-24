using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiClientServer
{
    public class ConnectionInputHandler : InputHandler
    {
        InputHandler programInputHandler = new ProgramInputHandler();

        public override void B(string[] input)
        {
            //Als het bericht voor mij is
            if (int.Parse(input[1]) == Program.MijnPoort)
            {
                Console.WriteLine(input[2]);
            }
            //Anders doorsturen naar preferred neighbour
            else
            {
                Console.WriteLine("Bericht voor " + input[1] + " doorgestuurd naar " + Program.readNbuv(int.Parse(input[1])));
                programInputHandler.B(input);
            }
        }

        public override void D(string[] input, bool sendMessage)
        {
            //Delete doen zonder bericht te sturen naar andere poort
            programInputHandler.D(input, false);

        }

        public override void myDist(string[] input)
        {
            while (Program.initKlaar == false) { }
            int u = int.Parse(input[1]);    //de buur die dit stuurt
            int v = int.Parse(input[2]);    //de node waarnaar zijn afstand is veranderd
            int d = int.Parse(input[3]);    //zijn nieuwe afstand daarnaartoe
            //Console.WriteLine("Ontvangen " + " " + u + " " + v + " " + d);
            Tuple<int, int> uv = new Tuple<int, int>(u, v);

            /*
            lock (Program.Duv)
            {
                if (!Program.Duv.Keys.Contains(v))
                {
                    Program.Duv.Add(v, Program.N + 1);          //nu een +1, kan later ook IN de recompute
                    lock (Program.NLocker)
                    {
                        // Program.N = 5 + Program.Duv.Count();
                    }
                }
            }*/
            
            Program.addOrSetNdisuwv(uv, d);         //toevoegen of wijzigen nieuwe d
            lock (Program.Buren)
            {
                lock (Program.recomputeLocker)
                {
                    Program.Recompute(v);       //en recompute
                }
            }
        }
    }
}
