using System;

namespace Bass.Internal
{
    internal class ExceptionLogger
    {
        public static void Trace(Exception e)
        {
#if DEBUG
            Console.Write(e.ToString());
#endif
        }
    }
}
