/*
 * primExport.cs :
 *
 * Main class for a simple prim exporter test.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using libsecondlife;

namespace primexport {
	public class primexport {
		private SecondLife	client;
		private Dictionary<ulong, Primitive> Prims = new Dictionary<ulong, Primitive>();
		private List<ulong> Attachments = new List<ulong>();

		// hard-coding is bad, but...
		private string firstName = "YourFirstName";
		private string lastName = "YourLastName";
		private string password = "YourPassword";
		private string loginUri = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";
		private string regionName = "aRegionName";
		
		/*
		private string firstName = "YourFirstName";
		private string lastName = "YourLastName";
		private string password = "YourPassword";
		private string loginUri = "http://osgrid.org:8002/";
		private string regionName = "Wright Plaza";
		*/

		private string clientName = "MyOpenClient";
		private string clientVersion = "0.002";
		private string outputFilename = "/tmp/prims.txt";

		private int sim_prims_limit = 0; // download this max amount of prims then logout (0 to disable and enable timer-based stop)
		private System.Timers.Timer ranEnough = new System.Timers.Timer(300000); // run for 5 mins max
		private System.Timers.Timer updateTimer = new System.Timers.Timer(60000);
		double heading = -Math.PI;
		private string sim_name = "";

		static void Main(string[] args) {
			primexport primExporter = new primexport();	
		}

		public primexport() {
			client = new SecondLife();
			client.Settings.LOGIN_SERVER = loginUri;
			client.Settings.MULTIPLE_SIMS = false;
			client.Settings.STORE_LAND_PATCHES = false;
			client.Throttle.Total = 15000000;
			// Throttle down unnecessary things
			client.Throttle.Cloud = 0;
			client.Throttle.Land = 0;
			client.Throttle.Wind = 0;
			string startLocation = NetworkManager.StartLocation(regionName, 103,223,27);

			client.Network.OnLogin += new NetworkManager.LoginCallback(Network_OnLogin);
			client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(Objects_OnNewPrim);
			client.Objects.OnNewAttachment += new ObjectManager.NewAttachmentCallback(AttachmentSeen);
			client.Network.OnSimConnected += new NetworkManager.SimConnectedCallback(Network_OnSimConnected);

			updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
			ranEnough.Elapsed += new System.Timers.ElapsedEventHandler(ranEnough_Elapsed);

			// fire-up client login
			client.Network.Login(firstName, lastName, password, clientName, startLocation, clientVersion);
		}

		private void exportToFile() {
			FileStream 	file = null;
			StreamWriter 	stream = null;
			string		output;

			try {
				file = new FileStream(outputFilename, FileMode.Create);
				stream = new StreamWriter(file);

				Console.WriteLine("Exporting primitives to file...");
				stream.WriteLine("<primitives count=\"" + sim_prims_limit + "\" region=\"" + sim_name + "\">");
				lock (Prims) {
					foreach (Primitive prim in Prims.Values) {
						output = "";
						LLVector3 position = prim.Position;
						LLQuaternion rotation = prim.Rotation;
						
						if (prim.ParentID != 0) {
							// This prim is part of a linkset, we need to adjust it's position and rotation
							if (Prims.ContainsKey(prim.ParentID)) {
								// The child prim only stores a relative position, add the world position of the parent prim
								position += Prims[prim.ParentID].Position;
								// same for rotation:
								rotation = rotation * Prims[prim.ParentID].Rotation;
							} else if (Attachments.Contains(prim.ParentID)) {
								// Skip this
							} else {
								Console.WriteLine("Can't export child prim ["+prim.ID.ToString()+"] - parent is missing.");
								continue;
							}
						}

						output += " <primitive name=\"Object\" description=\"\" key=\"Num_000" + prim.LocalID + "\" version=\"2\">" + Environment.NewLine;
						output += "  <properties>" + Environment.NewLine + "   <levelofdetail val=\"9\" />" + Environment.NewLine;
						LLObject.ObjectData data = prim.Data;
						output += "   <position x=\"" + string.Format("{0:F6}", position.X) +
							"\" y=\"" + string.Format("{0:F6}", position.Y) +
							"\" z=\"" + string.Format("{0:F6}", position.Z) + "\" />" + Environment.NewLine;
						output += "   <rotation x=\"" + string.Format("{0:F6}", rotation.X) +
							"\" y=\"" + string.Format("{0:F6}", rotation.Y) +
							"\" z=\"" + string.Format("{0:F6}", rotation.Z) +
							"\" s=\"" + string.Format("{0:F6}", rotation.W) + "\" />" + Environment.NewLine;
						output += "   <size x=\"" + string.Format("{0:F3}", prim.Scale.X) +
							"\" y=\"" + string.Format("{0:F3}", prim.Scale.Y) +
							"\" z=\"" + string.Format("{0:F3}", prim.Scale.Z) + "\" />" + Environment.NewLine;
						output += "   <profilecurve val=\"" + (uint)data.ProfileCurve + "\" />" + Environment.NewLine;
						output += "   <pathcurve val=\"" + (uint)data.PathCurve + "\" />" + Environment.NewLine;
						output += "   <pathbegin val=\"" + string.Format("{0:F6}", data.PathBegin) + "\" />" + Environment.NewLine;
						output += "   <pathend val=\"" + string.Format("{0:F6}", data.PathEnd) + "\" />" + Environment.NewLine;
						output += "   <profilebegin val=\"" + string.Format("{0:F6}", data.ProfileBegin) + "\" />" + Environment.NewLine;
						output += "   <profileend val=\"" + string.Format("{0:F6}", data.ProfileEnd) + "\" />" + Environment.NewLine;
						output += "   <profilehollow val=\"" + string.Format("{0:F6}", data.ProfileHollow) + "\" />" + Environment.NewLine;
						output += "   <pcode val=\"" + (uint)data.PCode + "\" />" + Environment.NewLine;
						output += "   <pathscalex val=\"" + string.Format("{0:F6}", data.PathScaleX) + "\" />" + Environment.NewLine;
						output += "   <pathscaley val=\"" + string.Format("{0:F6}", data.PathScaleY) + "\" />" + Environment.NewLine;
						output += "   <pathradiusoffset val=\"" + string.Format("{0:F6}", data.PathRadiusOffset) + "\" />" + Environment.NewLine;
						output += "   <pathrevolutions val=\"" + string.Format("{0:F6}", data.PathRevolutions) + "\" />" + Environment.NewLine;
						output += "   <pathshearx val=\"" + string.Format("{0:F6}", data.PathShearX) + "\" />" + Environment.NewLine;
						output += "   <pathsheary val=\"" + string.Format("{0:F6}", data.PathShearY) + "\" />" + Environment.NewLine;
						output += "   <pathtaperx val=\"" + string.Format("{0:F6}", data.PathTaperX) + "\" />" + Environment.NewLine;
						output += "   <pathtapery val=\"" + string.Format("{0:F6}", data.PathTaperY) + "\" />" + Environment.NewLine;
						output += "   <pathtwist val=\"" + string.Format("{0:F6}", data.PathTwist) + "\" />" + Environment.NewLine;
						output += "   <pathtwistbegin val=\"" + string.Format("{0:F6}", data.PathTwistBegin) + "\" />" + Environment.NewLine;
						output += "   <pathskew val=\"" + string.Format("{0:F6}", data.PathSkew) + "\" />" + Environment.NewLine;
						output += "  </properties>" + Environment.NewLine;
						output += " </primitive>" + Environment.NewLine;

						stream.WriteLine(output);
					}
				}
				stream.WriteLine("</primitives>");
			} catch (Exception e) {
				Console.WriteLine("Error writing primitives to file ["+outputFilename+"] :" + e.ToString());
			}
			finally {
				if (stream != null)
					stream.Close();
				if (file != null)
					file.Close();
			}
			Console.WriteLine("Export file written.");
		}

		private void Network_OnLogin(LoginStatus status, string message) {
			if (status == LoginStatus.Success) {
				Console.WriteLine("Login successful : " + message);
				updateTimer.Start();
				if (sim_prims_limit == 0) {
					ranEnough.Start();
				}
			} else if (status == LoginStatus.Failed) {
				Console.WriteLine("Login failed : " + message);
			}
		}

		/* to count total amount of primitives in sim */
		public void Network_OnSimConnected(Simulator sim) {
			Console.WriteLine("Connected to sim [" + sim.Name + "].");
			sim_name = sim.Name;
		}

		private void Objects_OnNewPrim(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
			lock (Prims) {
				if (Prims.ContainsKey(prim.LocalID)) {
					Prims.Remove(prim.LocalID);
				}
				Prims.Add(prim.LocalID, prim);
				Console.Write(".");
				if (Prims.Count % 1000 == 0) {
					Console.WriteLine("backuping...");
					exportToFile();
				}
				if (sim_prims_limit > 0 && Prims.Count >= sim_prims_limit) {
					Console.WriteLine("Found all " + sim_prims_limit + " primitives, exiting...");
					exportToFile();
					client.Network.Logout();
					Thread.Sleep(15000); // wait 15 seconds more
				}
			}
		}

		private void AttachmentSeen(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation) {
			lock (Attachments) {
				Attachments.Add(prim.LocalID);
			}
		}

		/* spin our camera in circles to get more prims */
		private void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			heading += 0.3d;
			if (heading > Math.PI) heading = -Math.PI;
			client.Self.Movement.UpdateFromHeading(heading, false);
		}

		private void ranEnough_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Console.WriteLine("Ran long enough, writing file and stopping...");
			exportToFile();
			client.Network.Logout();
			Thread.Sleep(15000); // wait 15 seconds more
		}

	}
}

