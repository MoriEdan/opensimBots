/*
 * primimport.cs :
 *
 * Main class for a try at a simple prim importer.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using libsecondlife;

namespace primimport {
	public class primimport {
		private SecondLife	client;
		//private Dictionary<ulong, LLObject.ObjectData> PrismsDatas = new Dictionary<ulong, LLObject.ObjectData>();
		//private Dictionary<ulong, Primitive> Prims = new Dictionary<ulong, Primitive>();

		// hard-coding is bad, but...
		private string firstName = "YourFirstName";
		private string lastName = "YourLastName";
		private string password = "YourPassword";
		private string loginUri = "http://osgrid.org:8002/";
		private string regionName = "aRegionName";
		
		private string clientName = "TestClient";
		private string clientVersion = "0.0001";
		private string inputFilename = "/tmp/prims.txt";

		private System.Timers.Timer ranEnough = new System.Timers.Timer(15000); // run for 15 seconds after rez completion
		private System.Timers.Timer startRezzing = new System.Timers.Timer(15000); // 15 seconds after sim connected

		static void Main(string[] args) {
			primimport primImporter = new primimport();	
		}

		public primimport() {
			client = new SecondLife();
			client.Settings.LOGIN_SERVER = loginUri;
			client.Settings.MULTIPLE_SIMS = false;
			client.Settings.STORE_LAND_PATCHES = false;
			string startLocation = NetworkManager.StartLocation(regionName, 128,128,31);

			client.Network.OnLogin += new NetworkManager.LoginCallback(Network_OnLogin);
			client.Self.OnInstantMessage += new AgentManager.InstantMessageCallback(Agent_OnInstantMessage);
			//client.Self.OnAlertMessage += new AgentManager.AlertMessage(OnAlertMessage);// won't work with opensim libsecondlife.dll
			ranEnough.Elapsed += new System.Timers.ElapsedEventHandler(ranEnough_Elapsed);
			startRezzing.Elapsed += new System.Timers.ElapsedEventHandler(startRezzing_Elapsed);
			client.Network.OnSimConnected += new NetworkManager.SimConnectedCallback(Network_OnSimConnected);

			// fire-up client login
			client.Network.Login(firstName, lastName, password, clientName, startLocation, clientVersion);
		}

		private void Network_OnLogin(LoginStatus status, string message) {
			if (status == LoginStatus.Success) {
				Console.WriteLine("Login successful : " + message);
			} else if (status == LoginStatus.Failed) {
				Console.WriteLine("Login failed : " + message);
			}
		}

		/* Triggered when a new connection to a simulator is established  */
		public void Network_OnSimConnected(Simulator sim) {
			Console.WriteLine("Connected to simulator [" + sim.Name + "].");
			startRezzing.Start();
		}

		public void Agent_OnInstantMessage(InstantMessage im, Simulator sim) {
			Console.WriteLine("Message received from sim " + sim.Name + " : " + im.Message);
		}

/*
 * won't work with opensim-compatible libsecondlife.dll builds
		public void OnAlertMessage(string message) {
			Console.WriteLine("Alert message received from grid : " + message);
		}
*/


		private void readInputFile() {
			FileStream stream = new FileStream(inputFilename, FileMode.Open);
			XmlReader reader = new XmlTextReader(stream);
			Console.WriteLine("Rezzing primitives from xml file...");
			LLObject.ObjectData primData = new LLObject.ObjectData();
			ulong LocalID = 0;
			LLVector3 primPos = client.Self.SimPosition;
			LLVector3 primScale = new LLVector3(1,1,1);
			LLQuaternion primRotation = new LLQuaternion(0,0,0);
			Simulator primSim = client.Network.CurrentSim;
			LLUUID primGroupID = LLUUID.Zero;
			while(reader.Read()) {
				switch (reader.NodeType) 
				{
					case XmlNodeType.Element :
						if (reader.Name == "primitive") {
							primData = new LLObject.ObjectData();
							primData.Material = LLObject.MaterialType.Wood;
							LocalID++;
						}
						if (reader.Name == "position") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "x")
									primPos.X = float.Parse(reader.Value);
								if (reader.Name == "y")
									primPos.Y = float.Parse(reader.Value);
								if (reader.Name == "z")
									primPos.Z = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "rotation") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "x")
									primRotation.X = float.Parse(reader.Value);
								if (reader.Name == "y")
									primRotation.Y = float.Parse(reader.Value);
								if (reader.Name == "z")
									primRotation.Z = float.Parse(reader.Value);
								if (reader.Name == "s")
									primRotation.W = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "size") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "x")
									primScale.X = float.Parse(reader.Value);
								if (reader.Name == "y")
									primScale.Y = float.Parse(reader.Value);
								if (reader.Name == "z")
									primScale.Z = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "profilecurve") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.ProfileCurve = (libsecondlife.LLObject.ProfileCurve)int.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathcurve") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathCurve = (libsecondlife.LLObject.PathCurve)int.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathbegin") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathBegin = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathend") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathEnd = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "profilebegin") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.ProfileBegin = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "profileend") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.ProfileEnd = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "profilehollow") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.ProfileHollow = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pcode") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PCode = (libsecondlife.PCode)int.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathscalex") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathScaleX = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathscaley") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathScaleY = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathradiusoffset") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathRadiusOffset = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathrevolutions") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathRevolutions = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathshearx") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathShearX = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathsheary") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathShearY = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathtaperx") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathTaperX = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathtapery") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathTaperY = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathtwist") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathTwist = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathtwistbegin") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathTwistBegin = float.Parse(reader.Value);
							}
						}
						if (reader.Name == "pathskew") {
							while (reader.MoveToNextAttribute()) {
								if (reader.Name == "val")
									primData.PathSkew = float.Parse(reader.Value);
							}
						}
	
						break;

					case XmlNodeType.EndElement :
						if (reader.Name == "primitive") {
							client.Objects.AddPrim(primSim, primData, primGroupID, primPos, primScale, primRotation);
							Console.WriteLine("Sleeping between prims... (" + LocalID + " rezzed)");
							Thread.Sleep(800);
						}
						break;
					/*
					case XmlNodeType.Text: //Display the text in each element.
						Console.WriteLine (reader.Value);
						break;
					*/
				}
			}
			Console.WriteLine("Found " + LocalID + " primitives to rez.");
		}

		private void startRezzing_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			startRezzing.Stop();
			Console.WriteLine("startRezzing timer elapsed : start rezzing from xml file...");
			readInputFile();
			Console.WriteLine("Done rezzing prims.");
			ranEnough.Start();
		}

		private void ranEnough_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Console.WriteLine("Ran long enough, stopping...");
			client.Network.Logout();
			Thread.Sleep(15000); // wait 15 seconds more
		}

	}
}
