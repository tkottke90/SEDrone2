public class CameraGroup {
	public List<IMyCameraBlock> group { get; } = new List<IMyCameraBlock>();

	public CameraGroup(){}

	public void Add(IMyCameraBlock cam){
		System.Text.RegularExpressions.Match tag = tag_match.Match(cam.CustomName);
		cam.CustomName = "AI-Camera" + " " + tag.Value;
		cam.EnableRaycast = true; 
		group.Add(cam);
	}

	public GPSlocation scan(IMyCameraBlock cam){
		if(cam.CanScan(SCAN_DISTANCE)){
			MyDetectedEntityInfo info = cam.Raycast(SCAN_DISTANCE,PITCH,YAW);
			if(info.HitPosition.HasValue) {
				GPSlocation ent = new GPSlocation(info.EntityId.ToString(), info.Position);
				ent.setCustomInfo("Type", info.Type.ToString(), true);
				ent.setCustomInfo("Size", (info.BoundingBox.Size.ToString("0.000")), true);
				ent.setCustomInfo("DisplayName", (info.Name), true);
				if(info.Relationship.ToString() != "NoOwnership"){
					ent.setCustomInfo("Owner", (info.Relationship.ToString()), true);
				}
				if(info.Velocity != new Vector3(0.0f, 0.0f, 0.0f)){
					ent.setCustomInfo("Velocity", info.Velocity.ToString("0.000"), true);
				}
				return ent;
			}
		}
		return null;
	}
}