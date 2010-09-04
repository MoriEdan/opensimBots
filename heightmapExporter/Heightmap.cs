using System;
using System.Collections.Generic;
using System.Threading;

namespace Heightmap
{
	static class Heightmap
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args) {
				string		line;
				frmHeightmap	bot;

				string		firstname = "botFirstname";
				string		lastname = "botLastname";
				string		password = "yourbotPass";
				string		loginuri = "http://yourgrid.com:8002/";
				string		inputfile = "regions.ini"; // list of regions to scan
				string		[] tokens;

				if (args.Length == 1) {
					inputfile = args[0];
				} else {
					Console.WriteLine("You could change input file path by passing it as first arg.");
				}

				// getting regions names from flat text file :
				System.IO.StreamReader file = new System.IO.StreamReader(inputfile);
				while ((line = file.ReadLine()) != null) {
					tokens = line.Split(','); // lineformat: region_name,x,y
					Console.WriteLine("Scanning region ["+tokens[0]+"] "
						+ "(x="+tokens[1]
						+ " y="+tokens[2] 
						+ ") ...");
					bot = new frmHeightmap(firstname, lastname, password, loginuri, tokens[0], int.Parse(tokens[1]), int.Parse(tokens[2]));
					while (!bot.complete_download) {
						Thread.Sleep(1000);
					}
					//bot.ForceLogout();
					Console.WriteLine("Bot finished his job at "+tokens[0]+", waiting 45secs ...");
					Thread.Sleep(45000);
					//System.GC.Collect();
				}
				Console.WriteLine("Done.");
			}
	}
}
