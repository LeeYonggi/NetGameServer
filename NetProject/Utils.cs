using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetProject
{
    public class Utils
    {
        static Utils instance;

        static public void DebugLog(string log)
        {
            // Console.ReadKey();
            Console.WriteLine(log);
        }
    }
}
