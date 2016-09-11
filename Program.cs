using System.Threading.Tasks;

namespace WhatToWatch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(async () => await new WhatToWatch().Run(args)).Wait();
        }
    }
}
