//Edited NoisyLittleBurger's Bjorklund Algorithm in C
//http://www.noisylittlebugger.net/diy/bjorklund/Bjorklund_Working_Final/Bjorklund_algorithm_arduino.txt
//CHANGED :
//1. use dynamic array.
//2. fixed sequence's off-spot problem
//3. added Explanation about Algorithm based on G.Touissant's Paper,
//"The Euclidean Algorithm Generates Traditional Musical Rhythms"

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MPTKDemoEuclidean
{
    public class BjorklundAlgo
    {
        public List<bool> Sequence;

        List<int> remainder;
        List<int> count;

        int step;
        int fill;

        public void Generate(int step, int fill, int offset = 0)
        {
            try
            {
                this.step = step;
                this.fill = fill;

                remainder = new List<int>();
                count = new List<int>();
                Sequence = new List<bool>();

                if (fill == 0)
                {
                    for (int i = 0; i < step; i++) Sequence.Add(false);
                }
                else if (fill >= step)
                {
                    for (int i = 0; i < step; i++) Sequence.Add(true);
                }
                else
                {
                    calculate();
                    //Print("calculate");
                    if (offset > 0) Offset(offset);
                }
                //Print();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("BjorklundAlgo error:" + ex.Message);
            }
        }

        public void Offset(int offset)
        {
            bool[] newseq = new bool[Sequence.Count];
            for (int i = 0; i + offset < Sequence.Count; i++)
                newseq[i + offset] = Sequence[i];

            for (int i = Sequence.Count - offset; i < Sequence.Count; i++)
                newseq[i - (Sequence.Count - offset)] = Sequence[i];

            Sequence = new List<bool>(newseq);
            //Print("after offset");
        }

        void calculate()
        {
            // Bjorklund algorithm
            // do E[k,n]. k is number of one's in sequence, and n is the length of sequence.
            int divisor = step - fill; //initial amount of zero's
            if (divisor < 0)
            {
                Debug.LogWarning("BjorklundAlgo: Divisor has to be greator than 0");
                return;
            }

            remainder.Add(fill);
            int index = 0;

            while (true)
            {
                count.Add((int)Mathf.Floor(divisor / remainder[index]));
                remainder.Add(divisor % remainder[index]);
                divisor = remainder[index];
                index++;
                if (remainder[index] <= 1)
                    break;
            }
            count.Add(divisor);
            buildSeq(index);
            Sequence.Reverse();

            //position correction. step rotated if first position is not fill.
            int zeroCount = 0;
            if (Sequence[0] != true)
            {
                // Search first 1
                do
                {
                    zeroCount++;
                }
                while (Sequence[zeroCount] == false && zeroCount < Sequence.Count);

                if (zeroCount < Sequence.Count)
                {
                    bool[] newseq = new bool[Sequence.Count];

                    for (int i = zeroCount; i < Sequence.Count; i++)
                        newseq[i - zeroCount] = Sequence[i];
                    for (int i = 0; i < zeroCount; i++)
                        newseq[Sequence.Count - zeroCount + i] = Sequence[i];
                    Sequence = new List<bool>(newseq);
                }
            }
        }
        void buildSeq(int slot)
        {
            //construct a binary sequence of n bits with k one‚ such that the k one‚ are distributed as evenly as possible among the zero

            if (slot == -1)
            {
                Sequence.Add(false);
            }
            else if (slot == -2)
            {
                Sequence.Add(true);
            }
            else
            {
                for (int i = 0; i < count[slot]; i++)
                    buildSeq(slot - 1);
                if (remainder[slot] != 0)
                    buildSeq(slot - 2);
            }
        }
        public void Print(string info, List<bool> seq = null)
        {
            if (seq == null) seq = Sequence;
            string result = string.Format("Seq({0},{1})=", fill, step);
            foreach (bool s in seq)
            {
                result += s ? "1" : "0";
            }
            Debug.Log(info + " " + result);
        }

    }
}