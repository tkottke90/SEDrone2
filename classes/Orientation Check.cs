const int CONFIG_RUNTIME = 90; 
 
StringBuilder sb = new StringBuilder();    
   
int run_count = 0; 
int run_time = 0; 
int rotate_pause = 0; 
string spinStatus; 
 
IMyGyro spin;  
IMyRemoteControl remote; 
 
Vector3D origin; 
double myX; double deltaX; 
double myY; double deltaY; 
double myZ; double deltaZ; 
 
IEnumerator<bool> _stateMachine; 
bool task_complete = false; 
int cycleStartTime = 0; 
  
const string tag_pattern = @"(\[AI)(\s\b[a-zA-z-]*\b)*(\])";   
                            
static System.Text.RegularExpressions.Regex tag_match = new System.Text.RegularExpressions.Regex(tag_pattern);   
static string TAG_PREFIX = "AI";  
    
public Program() {        
    Me.CustomData = "";    
    spinStatus = "Booting...."; 
    Echo("Booting....");
 
    List<IMyGyro> gyros = new List<IMyGyro>();  
    List<ITerminalAction> resultList = new List<ITerminalAction>(); 
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);  
  
    foreach(IMyGyro gyro in gyros){  
        try { 
            if(tag_match.IsMatch(gyro.CustomName)){   
                spin = gyro; 
                System.Text.RegularExpressions.Match tag = tag_match.Match(spin.CustomName);   
                spin.CustomName = "AI-Gyro " +  tag;  
            }  
        }catch(Exception e) { Me.CustomData = "Exception Found: " + e; } 
    }  
 
    List<IMyTerminalBlock> r = new List<IMyTerminalBlock>();     
    GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(r);      
        
    remote = (IMyRemoteControl)r[0]; 
     
    if(spin != null && remote != null) { 
        _stateMachine = rotate(); 
        origin = remote.GetPosition(); 
        myX = origin.X; 
        myY = origin.Y; 
        myZ = origin.Z; 
    } else if(spin == null) { 
        Echo("Missing Gyroscope"); 
    } else if(remote == null) { 
        Echo("Missing Remote Control"); 
    } 
}    
    
public void Main(string argument) { 
    if(argument != ""){  
        string[] args = argument.Split(' '); 
 
        if(args[0] == "scan") { Runtime.UpdateFrequency = UpdateFrequency.Update1;  } 
        if(args[0] == "stop") { Runtime.UpdateFrequency &= UpdateFrequency.Update1; } 
     } 
 
    //task_complete = false; 
    myX = remote.GetPosition().X; 
    myY = remote.GetPosition().Y; 
    myZ = remote.GetPosition().Z;         
 
    if(_stateMachine != null){ 
        if(!_stateMachine.MoveNext()){ 
            _stateMachine.Dispose(); 
            _stateMachine = null; 
            Me.CustomData = sb.ToString(); 
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;    
        } else { 
 
        } 
    } 
 
    Echo("Orientation Steps:"); 
    Echo("Stage: " + spinStatus); 
    Echo("Run Time: " + ticksinSeconds(run_count));  
    Echo("Run_Count: " + run_count);  
    Echo("Cycle Start Time: " + ticksinSeconds(run_count - cycleStartTime)); 
    Echo("Runtime Instructions: " + Runtime.CurrentInstructionCount); 
 
    run_time = ticksinSeconds(run_count); 
    run_count++;   
}  
  
public int ticksinSeconds(int ticks){  
    return ((ticks * 16)/1000);  
} 
 
private bool locationCheck(){ 
    bool boolX = Math.Round(myX, 2) == Math.Round(origin.X, 2); 
    bool boolY = Math.Round(myY, 2) == Math.Round(origin.Y, 2); 
    bool boolZ = Math.Round(myZ, 2) == Math.Round(origin.Z, 2); 
 
    if(boolX && boolY && boolZ){ return true;} else { return false; } 
} 
 
public void setSpin(float pitch, float yaw, float roll){ 
    spin.SetValue("Pitch", pitch); 
    spin.SetValue("Yaw", yaw); 
    spin.SetValue("Roll", roll); 
} 
 
public IEnumerator<bool> rotate(){ 
    // Start 
        rotate_pause = 0; 
        spin.ApplyAction("Override"); 
        setSpin(0.0f, 0.0f, 0.0f); 
        spinStatus = "Starting Scan"; sb.AppendLine(ticksinSeconds(run_count) + " - Task: Start"); 
        task_complete = true; 
        yield return true; 
    // Pitch 
        // Init 
        rotate_pause = 0; 
        setSpin(0.1f, 0.0f, 0.0f); 
        spinStatus = "Pitch-Scan"; sb.AppendLine(ticksinSeconds(run_count) + " - Task: Pitch-Scan");  
        cycleStartTime = run_count; 
        task_complete = false; 
        yield return true; 
        // Task 
        while(!task_complete){         
            sb.AppendLine("2 - rotate_pause: " + rotate_pause); 
            if (ticksinSeconds(rotate_pause) > 5 ) {     
                bool LC = locationCheck(); 
                sb.AppendLine("Location Check: " + LC); 
                if(ticksinSeconds(run_count - cycleStartTime) == CONFIG_RUNTIME || LC) {  
                    spin.SetValue("Pitch", 0.0f); 
                    task_complete = true; 
                } 
            } else { 
                rotate_pause++; 
            } 
 
            yield return true; 
        } 
         
    // Roll 
        // Init 
        rotate_pause = 0; 
        setSpin(0.0f, 0.0f, 0.1f); 
        spinStatus = "Roll-Scan"; sb.AppendLine(ticksinSeconds(run_count) + " - Task: Roll-Scan");  
        task_complete = false; 
        cycleStartTime = run_count; 
        yield return true; 
        // Task 
        while(!task_complete){        
            if (ticksinSeconds(rotate_pause) > 5 ) {     
                bool LC = locationCheck(); 
                if(ticksinSeconds(run_count - cycleStartTime) == CONFIG_RUNTIME || LC) {   
                    spin.SetValue("Roll", 0.0f); 
                    task_complete = true; 
                } 
            } else { 
                rotate_pause++; 
            } 
 
            yield return true; 
        } 
    // Yaw 
        // Init 
        rotate_pause = 0; 
        setSpin(0.0f, 0.1f, 0.0f); 
        spinStatus = "Yaw-Scan"; sb.AppendLine(ticksinSeconds(run_count) + " - Task: Yaw-Scan");  
        task_complete = false; 
        cycleStartTime = run_count; 
        yield return true; 
        // Task 
        while(!task_complete){ 
            if (ticksinSeconds(rotate_pause) > 5 ) {     
                bool LC = locationCheck(); 
                if(ticksinSeconds(run_count - cycleStartTime) == CONFIG_RUNTIME || LC) {  
                    spin.SetValue("Yaw", 0.0f); 
                    task_complete = true; 
                } 
            } else { 
                rotate_pause++; 
            } 
 
            yield return true; 
        } 
    // End 
        spin.ApplyAction("Override"); 
        spinStatus = "Scan Complete"; sb.AppendLine(ticksinSeconds(run_count) + " - Task: Yaw-Scan"); 
        yield return true; 
} 
 
public IEnumerator<bool> scanState() { 
    // Charging 
 
    yield return true; 
    // Scanning 
 
    yield return true; 
}