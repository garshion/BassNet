using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Server
{
	internal class Program
	{
		static void Main(string[] args)
		{
			SimpleChatServer server = new SimpleChatServer();
			server.Start();
		}
	}
}
