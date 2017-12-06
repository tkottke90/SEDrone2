public class LCDGroup {
	
	public string group_name = "";
	public List<IMyTextPanel> group { get; } = new List<IMyTextPanel>();
	
	public LCDGroup (string groupName){
		group_name = groupName;
	}
	
	public bool Add(IMyTextPanel txt){
		System.Text.RegularExpressions.Match tag = tag_match.Match(txt.CustomName);
		txt.CustomName = "AI-" + group_name + " " + tag.Value;
		int groupCount = group.Count;
		group.Add(txt);
		return group.Count == (groupCount + 1);
	}
	
	public void writeToLCD(string output){                                  
		foreach(IMyTextPanel lcd in group){
			((IMyTextPanel)lcd).WritePublicText(output,false);                  
			((IMyTextPanel)lcd).ShowPublicTextOnScreen();                  
		}
	}  
 
	public void writeToLine(string output){               
		foreach(IMyTextPanel lcd in group){
			string txtOut = output + NewLine;               
			((IMyTextPanel)lcd).WritePublicText(output,true);                  
			((IMyTextPanel)lcd).ShowPublicTextOnScreen();     
		}
	}
}