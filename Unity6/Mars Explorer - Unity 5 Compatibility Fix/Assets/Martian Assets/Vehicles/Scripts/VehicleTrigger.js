private var vehicle : Vehicle;

function isLocalAuthority() : boolean {
	if(!ModernizationConfig.MultiplayerEnabled) return true;
	if(!vehicle || !vehicle.GetComponent.<NetworkView>()) return true;
	return vehicle.GetComponent.<NetworkView>().isMine;
}

function Start() {
	vehicle = transform.root.GetComponent(Vehicle);
}

function OnTriggerStay (other : Collider) {
	if(other.gameObject.layer == 14 || !isLocalAuthority()) return;
	if(other.attachedRigidbody) vehicle.OnRam(other.attachedRigidbody.gameObject);
}
