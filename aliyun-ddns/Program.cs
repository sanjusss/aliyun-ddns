using System;

namespace aliyun_ddns
{
    class Program
    {
        static void Main(string[] args)
        {
            DomainUpdater updater = new DomainUpdater();
            updater.Run();
#if DEBUG
            Console.Read();
#endif
        }
    }
}
