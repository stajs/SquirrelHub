using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquirrelHub.Packager
{
	class Program
	{
		static void Main(string[] args)
		{
			var releasifyer = new Releasifyer();

			Console.WriteLine("Relevant paths:");

			Console.WriteLine(releasifyer.PackagerDirectory);
			Console.WriteLine(releasifyer.BuildDirectory);
			Console.WriteLine(releasifyer.ClientDirectory);
			Console.WriteLine(releasifyer.ClientBinDirectory);
			Console.WriteLine(releasifyer.Squirrel);
			Console.WriteLine(releasifyer.AppIcon);
			Console.WriteLine();

			releasifyer.Releasify();

			Console.WriteLine("\n\nPress any key to exit.");
			Console.ReadLine();
		}
	}
}
