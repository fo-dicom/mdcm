using System;
using Dicom.Network;
using Dicom.Network.Client;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Monotouch.Dicom.Viewer
{
	public partial class AppDelegate
	{
		
		public void EchoDicomServer ()
		{
			RootElement root = null;
			Section resultSection = null;
			
			var hostEntry = new EntryElement("Host", "Host name of the DICOM server", "server");
			var portEntry = new EntryElement("Port", "Port number", "104");
			var calledAetEntry = new EntryElement("Called AET", "Called application AET", "ANYSCP");
			var callingAetEntry = new EntryElement("Calling AET", "Calling application AET", "ECHOSCU");

			var echoButton = new StyledStringElement("Click to test", 
			delegate {
				if (resultSection != null) root.Remove(resultSection);
				string message;
				var echoFlag = DoEcho(hostEntry.Value, Int32.Parse(portEntry.Value), calledAetEntry.Value, callingAetEntry.Value, out message);
				var echoImage = new ImageStringElement(String.Empty, echoFlag ? new UIImage("yes-icon.png") : new UIImage("no-icon.png"));
				var echoMessage = new StringElement(message);
				resultSection = new Section(String.Empty, "C-ECHO result") { echoImage, echoMessage };
				root.Add(resultSection);
			})
			{ Alignment = UITextAlignment.Center, BackgroundColor = UIColor.Blue, TextColor = UIColor.White };
			
			root = new RootElement("Echo DICOM server") {
				new Section { hostEntry, portEntry, calledAetEntry, callingAetEntry},
				new Section { echoButton },
				
			};
			
			var dvc = new DialogViewController (root, true) { Autorotate = true };
			navigation.PushViewController (dvc, true);
		}
		
		private bool DoEcho(string host, int port, string calledAet, string callingAet, out string message)
		{
			var success = false;
			var msg = "Unidentified failure";
			
			var echoScu = new CEchoClient { CalledAE = calledAet, CallingAE = callingAet };
			echoScu.OnCEchoResponse += delegate(byte presentationID, ushort messageID, DcmStatus status)
			{
				success = status.State == DcmState.Success; msg = status.Description;
			};
			
			echoScu.Connect(host, port, DcmSocketType.TCP);
			if (!echoScu.Wait())
			{
				msg = echoScu.ErrorMessage;
			}
			
			message = msg;
			return success;
		}
	}
}

