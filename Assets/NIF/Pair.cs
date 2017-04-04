using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class Pair<T, U>
    {
        public Pair()
        {
        }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }

        static public Pair<T, U> of(T first, U second)
        {
            return new NIF.Pair<T, U>(first, second);
        }
        public T getKey() { return First;  }
        public U getValue() { return Second; }
        public T First { get; set; }
        public U Second { get; set; }
    };
}
