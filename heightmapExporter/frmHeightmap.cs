using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Threading;
using libsecondlife;
using libsecondlife.Packets;

namespace Heightmap
{
	public class frmHeightmap
	{
		private string picOutputPath = "/tmp/";
		private SecondLife Client = new SecondLife();
		Bitmap landTerrain;
		private System.Timers.Timer TimeoutTimer = new System.Timers.Timer(300000); // will run for 300 secs max
		private string FirstName, LastName, Password, SimName;
		private int terrain_patches_received = 0;
		private int sim_x, sim_y;
		public bool complete_download = false;

		double heading = -Math.PI;

		public frmHeightmap(string firstName, string lastName, string password, string loginUri, string simName, int simX, int simY)
		{
			FirstName = firstName;
			LastName = lastName;
			Password = password;
			SimName = simName;
			sim_x = simX;
			sim_y = simY;

			landTerrain = new Bitmap(256, 256, PixelFormat.Format24bppRgb);
			Client.Settings.LOGIN_SERVER = loginUri;
			Client.Network.OnLogin += new NetworkManager.LoginCallback(Network_OnLogin);

			// Throttle land up and other things down
			Client.Throttle.Cloud = 0;
			Client.Throttle.Land = 1000;
			Client.Throttle.Wind = 0;
			Client.Settings.MULTIPLE_SIMS = false;
			Client.Settings.ENABLE_CAPS = false;
			Client.Settings.LOGOUT_TIMEOUT = 10000;
			Client.Settings.ALWAYS_DECODE_OBJECTS = false;
			Client.Settings.ALWAYS_REQUEST_OBJECTS = false;
			Client.Settings.OBJECT_TRACKING = false;
			Client.Settings.PARCEL_TRACKING = false;
			Client.Settings.DEBUG = false;
			Client.Settings.STORE_LAND_PATCHES = true;

			// prevent infinite running (not sure to receive full map)
			TimeoutTimer.Elapsed += new System.Timers.ElapsedEventHandler(TimeoutTimer_Elapsed);
			TimeoutTimer.Start();

			Client.Terrain.OnLandPatch += new TerrainManager.LandPatchCallback(Terrain_OnLandPatch);
			string startLocation = NetworkManager.StartLocation(SimName, 128,128,128);
			Client.Network.Login(FirstName, LastName, Password, "MapperBot", startLocation, "0.01");
		}

		private void Network_OnLogin(LoginStatus login, string message)
		{
			if (login == LoginStatus.Success)
			{
				Console.WriteLine("Login successful");
			}
			else if (login == LoginStatus.Failed)
			{
				Console.WriteLine("Login failed: " + Client.Network.LoginMessage);
				return;
			}
		}

		void TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Console.WriteLine("Max runtime elapsed.. aborting.");
			Client.Network.Logout();
			complete_download = true;
		}

		public void ForceLogout() {
			Client.Network.Shutdown(NetworkManager.DisconnectType.ClientInitiated);
		}

/*
		void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			// Spin our camera in circles at the center of the sim to load all the terrain
			heading += 0.3d;
			if (heading > Math.PI) heading = -Math.PI;

			Client.Self.Movement.UpdateFromHeading(heading, false);
		}
*/
		void Terrain_OnLandPatch(Simulator simulator, int x, int y, int width, float[] data)
		{
			lock (landTerrain) {
				if (x >= 16 || y >= 16) {
					Console.WriteLine("Bad patch coordinates, x = " + x + ", y = " + y);
					return;
				}
				if (width != 16) {
					Console.WriteLine("Unhandled patch size " + width + "x" + width);
					return;
				}

				if (terrain_patches_received < 256) { // prevent dup updates ?
					for (int yp = 0; yp < 16; yp++)
					{
						for (int xp = 0; xp < 16; xp++)
						{
							float height = data[yp * 16 + xp];
							int colorVal = Helpers.FloatToByte(height, 0.0f, 60.0f);
							int lesserVal = (int)((float)colorVal * 0.75f);
							Color color;

							if (height >= simulator.WaterHeight)
								color = Color.FromArgb(lesserVal, colorVal, lesserVal);
							else
								color = Color.FromArgb(lesserVal, lesserVal, colorVal+60);
							landTerrain.SetPixel((x*16)+xp, 256 - ((y*16)+yp), color);
						}
					}
					terrain_patches_received++;
					Console.Write(".");
				}
				if (terrain_patches_received == 256) {
					terrain_patches_received = 4242;
					Console.WriteLine("Done.");
					Console.WriteLine("Got all 256 land patches, saving image...");
					TimeoutTimer.Stop();
					Thread.Sleep(5000);
					SaveTextBitmap(landTerrain);
					Console.WriteLine("Waiting a few seconds before logging out (prevent ghosting).");
					Thread.Sleep(15000);
					Client.Network.Logout();
					Thread.Sleep(10000);
					TimeoutTimer.Dispose();
					complete_download = true;
				}
			}
		}

		private void drawGraphicString(string str, Graphics g, int x, int y, int fontSize) {
			Font fontDesc = new Font("Arial", fontSize);
			SizeF stringSize = g.MeasureString(str,fontDesc);
			int width = (int) stringSize.Width;
			int height = (int) stringSize.Height;
			if (width > 246) { width = 246; }
			if (height > 256) { height = 256; }
			StringFormat stringFormat = new StringFormat();
			stringFormat.Alignment = StringAlignment.Near;
			stringFormat.LineAlignment = StringAlignment.Center;
			g.DrawString(str, fontDesc, new SolidBrush(Color.White), new Rectangle(x, y, width, height), stringFormat);
		}

		/* Adds text to bitmap and saves it under another name/path  */
		public void SaveTextBitmap(Bitmap bitmap) {
			Bitmap bmp = new Bitmap(256,256);
			Graphics g = Graphics.FromImage(bmp);
			g.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, 255, 255), 0, 0, 256,256, GraphicsUnit.Pixel);
			drawGraphicString(SimName, g, 2, 2, 24);
			drawGraphicString("x:"+sim_x, g, 5, 40, 18);
			drawGraphicString("y:"+sim_y, g, 5, 70, 18);
			try {
				bmp.Save(picOutputPath+sim_x+"-"+sim_y+".jpg",
					System.Drawing.Imaging.ImageFormat.Jpeg);
				Console.WriteLine("Saved image "+picOutputPath+sim_x+"-"+sim_y+".jpg");
			} catch (Exception e) {
				Console.WriteLine("An exception was thrown : " + e.Message);
			} finally {
				g.Dispose();
			}
		}

	}
}
