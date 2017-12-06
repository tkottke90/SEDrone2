/*
	Malware MDK - https://github.com/malware-dev/MDK-SE
 */

// Config:

	const char TAG_OPEN = '[', TAG_CLOSED = ']';
	static string TAG_PREFIX = "AI";
	static string SCAN_PREFIX = "SCAN-DISPLAY";
	static string DEBUG_PREFIX = "SCAN-DEBUG";
	static int REFRESH_TIME = 2;
	int GridType = 0;


// Variables:
	// Script Info:
	const string VERSION = "v1.0";

	// System:
	StringBuilder sb = new StringBuilder();
	bool firstrun = true;
	int runtime_count = 0;
	string script_startTime = "";
	int REFRESH_RATE = (REFRESH_TIME*60*1000)/16;
	int refresh_count = 0;
	
	int DEBUG = 1;
	static StringBuilder debugSB = new StringBuilder();

	const double SCAN_DISTANCE_PER_TICK = 0.032;
	static double SCAN_DISTANCE = 10000;
	// Raycast Charges at a rate of 32m/Tick or 32m/16ms or 0.032km/16ms
	// To calculate time to charge multiply SCAN_DISTANCE by SCAN_DISTANCE_PER_TICK
	// Default Value is 10km which takes 313 Ticks (~5.0 sec) to charge
	static int SCAN_RATE = Convert.ToInt32(SCAN_DISTANCE * SCAN_DISTANCE_PER_TICK);
	static int scan_charge = 0;
	static string status = "Idle";
	static float PITCH = 0;
	static float YAW = 0;
	
	const string tag_pattern = @"(\[AI)(\s\b[a-zA-z-]*\b)*(\s?\])";
	static System.Text.RegularExpressions.Regex tag_match = new System.Text.RegularExpressions.Regex(tag_pattern);

	// Display:
	LCDGroup lcdScan = new LCDGroup("Scan-Display");
	LCDGroup lcdDebug = new LCDGroup("Scan-Debug");

	// Navigation:
	CameraGroup cameras = new CameraGroup();
	List<IMyGyro> gyros = new List<IMyGyro>();
	List<IMyMotorStator> rotors = new List<IMyMotorStator>();
	Vector3D origin;
	Vector3D current;
	
	// Information:
	static Dictionary<string, GPSlocation> asteriods = new Dictionary<string, GPSlocation>();
	static Dictionary<string, GPSlocation> ships = new Dictionary<string, GPSlocation>();
	static Dictionary<string, GPSlocation> stations = new Dictionary<string, GPSlocation>();

// Constructor - Called Once on World Load
public Program()
{
	script_startTime = DateTime.Now.ToString("h:mm:ss tt");
	Echo("Status: Initializing");
	firstrun = !Refresh();
    // It's recommended to set RuntimeInfo.UpdateFrequency
    // here, which will allow your script to run itself without a
    // timer block.

	 Runtime.UpdateFrequency = UpdateFrequency.Update1;

    Echo("Status: Standby");
}

// Call to Update Persistant Variables
public bool Refresh(){
	bool result = false;
	// Check for LCD Panels
	
	getLCDs();
	result = getCameras();

	return result;
}

// Call To Save Data
public void Save(){
	// Generate Save String
	StringBuilder data = new StringBuilder();
	data.Append("asteriods:");
	foreach(KeyValuePair<string, GPSlocation> dat in asteriods){
		data.Append(dat.Value.ToString() + ",");
	}
	data.Append("\r\nships:");
	foreach(KeyValuePair<string, GPSlocation> dat in ships){
		data.Append(dat.Value.ToString() + ",");
	}
	data.Append("\r\nstations:");
	foreach(KeyValuePair<string, GPSlocation> dat in stations){
		data.Append(dat.Value.ToString());
	}

	Me.CustomData = data.ToString();
	Storage = data.ToString();
}

// Call to Load Data
public void Load(){
	// Get Prefs from Custom Data
	string Prefs = Me.CustomData;
	
	
	
	// Get Storage

}

public string Status() {
	return "";
}

public void Main(string argument, UpdateType updateSource)
{
	// Check if init is setup
	if(firstrun || refresh_count == REFRESH_RATE){
		Refresh();
	}
	// Load Variables
	Load();

	// Do Work
	if(scan_charge == SCAN_RATE || argument == "scan"){ 
		status = "Scanning"; 

		foreach(IMyCameraBlock cam in cameras.group){
			GPSlocation gps = cameras.scan(cam);
			if(gps != null){
				switch(gps.customInfo["Type"]){
					case "Asteroid":
							if(!asteriods.ContainsKey(gps.name)){
								asteriods.Add(gps.name, gps);
							}
						break;
					case "SmallGrid":
							if(!ships.ContainsKey(gps.name)) {
								ships.Add(gps.name, gps);
							}
						break;
					case "LargeGrid":
						if(gps.customInfo.ContainsKey("Velocity") && !ships.ContainsKey(gps.name)){	
								ships.Add(gps.name, gps);
						} else if(!stations.ContainsKey(gps.name)) {
							stations.Add(gps.name, gps);
						}
						break;
					default:
						debugLog("Non-Asteriod/Ship/Station Entity Found: " + gps.name);
						break;
				}
			}
		}

		scan_charge = 0; 
	} else { 
		status = "Charging"; 
	}

	// Draw Data
	lcdScan.writeToLCD(drawSCAN());
	lcdDebug.writeToLCD(drawDEBUG());

	// Save Variables
	Save();
	// Increment charge_counter
	runtime_count++;

	Echo("AI Scan Script:");
	Echo("Script Version: " + VERSION);
	Echo("Script Start Time: " + script_startTime);
	Echo("Runtime Count(Ticks): " + runtime_count);
	Echo("Runtime (sec): " + ((runtime_count * 16)/1000 ) + " sec");
	Echo("lcdScan Count: " + lcdScan.group.Count);
	Echo("Cameras: " + cameras.group.Count);
	Echo("Status: " + status);
	scan_charge++;
}

// ** Get Blocks ** //
public bool getLCDs(){
	bool result = false;
	List<IMyTerminalBlock> lcdList = new List<IMyTerminalBlock>();
	try {
		GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcdList);
		lcdScan.group.Clear();
		foreach(IMyTextPanel txt in lcdList){
		if(tag_match.IsMatch(txt.CustomName) && txt.CustomName.Contains(SCAN_PREFIX)){
				lcdScan.Add(txt);
				result = true;
			}
		}
	} catch(Exception e){
		Echo("getLCDS() Error getting lcdMAIN: " + e.ToString().Split(':')[0].Split('.')[1]);
		return false;
	}
	
	try {
		GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcdList);
		lcdDebug.group.Clear();
		foreach(IMyTextPanel txt in lcdList){
			if(tag_match.IsMatch(txt.CustomName) && txt.CustomName.Contains(DEBUG_PREFIX)){
				lcdDebug.Add(txt);
				result = true;
			}
		}
	} catch(Exception e){
		Echo("getLCDS() Error getting lcdDebug: " + e.ToString().Split(':')[0].Split('.')[1]);
		return false;
	}
	// Return status
	return result;
}

public bool getCameras() {
	List<IMyCameraBlock> camList = new List<IMyCameraBlock>();
	cameras.group.Clear();
	try {
		GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(camList);
		foreach(IMyCameraBlock cam in camList){
			if(tag_match.IsMatch(cam.CustomName)){
				cameras.Add(cam);
			}
		}
		return cameras.group.Count > 0;
	} catch(Exception e){
		Echo("getCameras() Error getting Cameras: " + e.ToString().Split(':')[0].Split('.')[1]);
		return false;
	}
}

public bool getGyros() {
	List<IMyGyro> gyroList = new List<IMyGyro>();
	gyros.Clear();
	try {
		GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyroList);
		foreach(IMyGyro gyro in gyroList){
			if(tag_match.IsMatch(gyro.CustomName)){
				gyros.Add(gyro);
			}
		}
		return true;
	} catch(Exception e){
		Echo("getGyros() Error getting Cameras: " + e.ToString().Split(':')[0].Split('.')[1]);
		return false;
	}
}

public bool getRotors() {
	List<IMyMotorStator> rotorList = new List<IMyMotorStator>();
	rotors.Clear();
	try {
		GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(rotorList);
		foreach(IMyMotorStator rot in rotorList){
			if(tag_match.IsMatch(rot.CustomName)){
				rotors.Add(rot);
			}
		}
		return true;
	} catch(Exception e){
		Echo("getRotors() Error getting Cameras: " + e.ToString().Split(':')[0].Split('.')[1]);
		return false;
	}
}

// ** Draw Functions ** //

public void writeToLCD(IMyTextPanel lcd, string output, bool append){                  
	// Applys text to LCD Screens                  
	if (lcd != null){
		((IMyTextPanel)lcd).WritePublicText(output,append);                  
		((IMyTextPanel)lcd).ShowPublicTextOnScreen();                  
	}
}  
 
public void writeToLine(IMyTextPanel lcd, string output, bool append){               
	string txtOut = output + "\r\n";               
	writeToLCD(lcd,txtOut,append);               
}

public string drawSCAN(){
	sb.AppendLine("AI Raycast Scanner").AppendLine();
	sb.AppendLine("Status: " + status);
	sb.AppendLine("Scanning Distance: " + SCAN_DISTANCE + " m");
	sb.AppendLine("Charge Time: " + (((SCAN_RATE - scan_charge) * 16)/1000 ) + " sec");
	sb.AppendLine().AppendLine("Located Asteriods: " + asteriods.Count);
	sb.AppendLine("Located Ships: " + ships.Count);
	sb.AppendLine("Located Stations: " + stations.Count);

	string output = sb.ToString();
	sb.Clear();
	return output;
}

public string drawDEBUG(){
	return debugSB.ToString();
}

public void debugLog(string log) {
	debugSB.AppendLine(log);
}

// Classes and Static Values

	const string NewLine = "\r\n";



public class GyroGroup {

	public IMyGyro activeGyro;
	public List<IMyGyro> group { get; } = new List<IMyGyro>();

	public GyroGroup(){}

	public bool Add(IMyGyro gyro){
		System.Text.RegularExpressions.Match tag = tag_match.Match(gyro.CustomName);
		int groupCount = group.Count;
		if(groupCount == 0) {
			setActive(gyro);
		} else {
			setInactive(gyro);
		}
		group.Add(gyro);
		return group.Count == (groupCount + 1);
	}

	public bool checkActive() {
		if(activeGyro == null || group.Count > 0){
			return false;
		} else {
			return true;
		}
	}

	public bool newActive() {
		return true;
	}

	public void setActive(IMyGyro gyro) {
		System.Text.RegularExpressions.Match tag = tag_match.Match(gyro.CustomName);
		string newTag = gyro.CustomName.Replace(tag.Value, "");
		string[] tag_parts = tag.Value.Split(' ');

		newTag += "[AI ";
		for(int i = 1; i < tag_parts.Length; i++){
			if(tag_parts[i] != "Inactive"){
				newTag += tag_parts[i] + " ";
			}
		}

		newTag += "Active ]";

		gyro.ApplyAction("OnOff_On");
		gyro.CustomName = newTag;
	}

	public void setInactive(IMyGyro gyro) {
		System.Text.RegularExpressions.Match tag = tag_match.Match(gyro.CustomName);
		string newTag = gyro.CustomName.Replace(tag.Value, "");
		string[] tag_parts = tag.Value.Split(' ');

		newTag += "[AI ";
		for(int i = 1; i < tag_parts.Length; i++){
			if(tag_parts[i] != "Active"){
				newTag += tag_parts[i] + " ";
			}
		}

		newTag += "Inactive ]";

		gyro.ApplyAction("OnOff_Off");
		gyro.CustomName = newTag;
	}

	public bool setSpin(IMyGyro gyro, float pitch, float yaw, float roll){
		try{
			gyro.SetValue("Pitch", pitch);
			gyro.SetValue("Yaw", yaw);
			gyro.SetValue("Roll", roll);
			return true;
		} catch(Exception e){
			debugSB.AppendLine("Set Spin Error:").AppendLine($"Exception: {e}\n---");
			return false;
		}
	}
}

public class CameraGroup {public List<IMyCameraBlock> group { get; } = new List<IMyCameraBlock>();public CameraGroup(){}public void Add(IMyCameraBlock cam){System.Text.RegularExpressions.Match tag = tag_match.Match(cam.CustomName);cam.CustomName = "AI-Camera" + " " + tag.Value;cam.EnableRaycast = true;group.Add(cam);}public GPSlocation scan(IMyCameraBlock cam){if(cam.CanScan(SCAN_DISTANCE)){MyDetectedEntityInfo info = cam.Raycast(SCAN_DISTANCE,PITCH,YAW);if(info.HitPosition.HasValue) {GPSlocation ent = new GPSlocation(info.EntityId.ToString(), info.Position);ent.setCustomInfo("Type", info.Type.ToString(), true);ent.setCustomInfo("Size", (info.BoundingBox.Size.ToString("0.000")), true);ent.setCustomInfo("DisplayName", (info.Name), true);if(info.Relationship.ToString() != "NoOwnership"){ent.setCustomInfo("Owner", (info.Relationship.ToString()), true);}if(info.Velocity != new Vector3(0.0f, 0.0f, 0.0f)){ent.setCustomInfo("Velocity", info.Velocity.ToString("0.000"), true);}return ent;}}return null;}}
public class LCDGroup {public string group_name = "";public List<IMyTextPanel> group { get; } = new List<IMyTextPanel>();public LCDGroup (string groupName){group_name = groupName;}public bool Add(IMyTextPanel txt){System.Text.RegularExpressions.Match tag = tag_match.Match(txt.CustomName);txt.CustomName = group_name + " " + tag.Value;int groupCount = group.Count;group.Add(txt);return group.Count == (groupCount + 1);}public void writeToLCD(string output){foreach(IMyTextPanel lcd in group){((IMyTextPanel)lcd).WritePublicText(output,false);((IMyTextPanel)lcd).ShowPublicTextOnScreen();}}public void writeToLine(string output){foreach(IMyTextPanel lcd in group){string txtOut = output + NewLine;((IMyTextPanel)lcd).WritePublicText(output,true);((IMyTextPanel)lcd).ShowPublicTextOnScreen();}}}
public class GPSlocation {public string name;public Vector3D gps;public int fitness = 0;public int fitnessType = 0;public Dictionary<string,string> customInfo = new Dictionary<string,string>();public string eventLog = "";public GPSlocation (string newName, Vector3D newGPS){name = newName;gps = newGPS;}public GPSlocation (string storedGPS){char[] charsToTrim = {'<','>',' '};string storeGPS = storedGPS.Trim();storeGPS = storeGPS.Trim(charsToTrim);string[] attr = storeGPS.Split('^');name = attr[0];gps = recoverGPS(attr[1]);int fit; bool fitCheck = Int32.TryParse(attr[2],out fit);if(fitCheck){fitness = fit;}else{fitness = 0;}if(attr.Length == 4){string[] customAttr = attr[3].Split('$');foreach(string str in customAttr){str.Trim(' ');if(str.Length > 3 || str != ""){string[] temp = str.Split(':');try{customInfo.Add(temp[0],temp[1]);}catch(Exception e){eventLog += String.Format("Error Adding: {3}\r\n \tKey: {0}\r\n \tValue: {1}\r\n \r\n Stack Trace:\r\n{2}\r\n",temp[0],"value",e.ToString(),str);}}}}}public MyWaypointInfo convertToWaypoint(){return new MyWaypointInfo(name,gps);}public Vector3D recoverGPS(string waypoint){waypoint = waypoint.Trim(new Char[] {'{','}'});string[] coord = waypoint.Split(' ');double x = double.Parse(coord[0].Split(':')[1]);double y = double.Parse(coord[1].Split(':')[1]);double z = double.Parse(coord[2].Split(':')[1]);return new Vector3D(x,y,z);}public bool checkNear(Vector3D gps2){double deltaX = (gps.X > gps2.X) ? gps.X - gps2.X : gps2.X - gps.X;double deltaY = (gps.Y > gps2.Y) ? gps.Y - gps2.Y : gps2.Y - gps.Y;double deltaZ = (gps.Z > gps2.Z) ? gps.Z - gps2.Z : gps2.Z - gps.Z;if(deltaX < 200 || deltaY < 200 || deltaZ < 200){return false;}else{return true;}}public string[] getCustomInfo(string infoName){string[] temp = {""};return temp;}public bool setCustomInfo(string infoName, string newValue, bool createNew) {if (createNew) {customInfo[infoName] = newValue;return true;} else {return false;}}public override string ToString(){string custom = "";if(customInfo.Count != 0){foreach (KeyValuePair<string,string> item in customInfo){custom += String.Format("{0}:{1}$",item.Key,item.Value);}custom = custom.TrimEnd('$');}else{custom = "0";}string rtnString = String.Format("<{0}^{1}^{2}^{3}>",name,gps.ToString(),fitness,custom);return rtnString;}}